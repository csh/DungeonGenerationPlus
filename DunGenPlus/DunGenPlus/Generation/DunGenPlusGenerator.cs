using DunGen.Graph;
using DunGen;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using DunGenPlus.Collections;
using DunGenPlus.Components;
using System.Security.Permissions;
using DunGenPlus.Managers;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using BepInEx.Logging;
using static UnityEngine.Rendering.HighDefinition.ScalableSettingLevelParameter;

[assembly: SecurityPermission( SecurityAction.RequestMinimum, SkipVerification = true )]
namespace DunGenPlus.Generation {
  internal class DunGenPlusGenerator {
    public static DunGenExtender Instance { get; internal set; }
    public static DunGenExtenderProperties Properties { get; internal set; }
    public static bool Active { get; internal set; }
    public static bool ActiveAlternative { get; internal set; }

    internal static HDRenderPipelineAsset previousHDRPAsset;
    internal static HDRenderPipelineAsset newHDRPAsset;

    public static void Activate(DungeonGenerator generator, DunGenExtender extender){
      Instance = extender;
      Active = true;
      ActiveAlternative = true;

      var props = extender.Properties.Copy();
      Instance.Events.OnModifyDunGenExtenderProperties.Invoke(props);
      props.SetupProperties(generator);
      Properties = props;

      if (Properties.UseDungeonBounds) {
        generator.DebugRender = true;
        generator.RestrictDungeonToBounds = Properties.UseDungeonBounds;
        var bounds = Properties.GetDungeonBounds(generator.LengthMultiplier);
        generator.TilePlacementBounds = bounds;
        Plugin.logger.LogDebug($"Dungeon Bounds: {bounds}");
      }

      if (Properties.UseMaxShadowsRequestUpdate) {
        Plugin.logger.LogDebug($"Updating HDRP asset: setting max shadows request to {Properties.MaxShadowsRequestAmount}");
        try {
          previousHDRPAsset = QualitySettings.renderPipeline as HDRenderPipelineAsset;
          newHDRPAsset = ScriptableObject.Instantiate(previousHDRPAsset);
      
          var settings = newHDRPAsset.currentPlatformRenderPipelineSettings;
          settings.hdShadowInitParams.maxScreenSpaceShadowSlots = Properties.MaxShadowsRequestAmount;
          newHDRPAsset.currentPlatformRenderPipelineSettings = settings;

          QualitySettings.renderPipeline = newHDRPAsset;
        } catch (Exception e) {
          Plugin.logger.LogError("Failed to update HDRP asset");
          Plugin.logger.LogError(e.ToString());
        }
      }


      DoorwayManager.ResetList();
    }

    public static void Deactivate(){
      Instance = null;
      Properties = null;
      Active = false;
      ActiveAlternative = false;

      if (previousHDRPAsset && QualitySettings.renderPipeline == newHDRPAsset) {
        Plugin.logger.LogDebug("Restoring original HDRP asset");

        QualitySettings.renderPipeline = previousHDRPAsset;
        previousHDRPAsset = null;
        newHDRPAsset = null;
      }
    }

    public static IEnumerator GenerateAlternativeMainPaths(DungeonGenerator gen) {
        
      var altCount = Properties.MainPathCount - 1;

      // default behaviour in case the multiple main paths are not considered
      if (!Active) {
        ActiveAlternative = false;
        yield return gen.Wait(gen.GenerateBranchPaths());
        ActiveAlternative = true;
        yield break;
      }

      if (altCount <= 0) {
        Plugin.logger.LogInfo($"Switching to default dungeon branch generation due to MainPathCount being {altCount + 1}");
        ActiveAlternative = false;
        yield return gen.Wait(gen.GenerateBranchPaths());
        ActiveAlternative = true;
        yield break;
      }

      if (Properties.MainRoomTilePrefab == null) {
        Plugin.logger.LogWarning($"Switching to default dungeon branch generation due to MainRoomTilePrefab being null");
        ActiveAlternative = false;
        yield return gen.Wait(gen.GenerateBranchPaths());
        ActiveAlternative = true;
        yield break;
      }

      var allMainPathTiles = new List<List<TileProxy>>();
      allMainPathTiles.Add(gen.proxyDungeon.MainPathTiles.ToList());

      // main room is the true main room and not the fake room
      // this MUST have multiple doorways as you can imagine
      var mainRoom = gen.proxyDungeon.MainPathTiles.FirstOrDefault(t => t.Prefab == Properties.MainRoomTilePrefab);
      if (mainRoom == null) {
        Plugin.logger.LogWarning($"Switching to default dungeon branch generation due to MainRoomTilePrefab not spawning on the main path");
        ActiveAlternative = false;
        yield return gen.Wait(gen.GenerateBranchPaths());
        ActiveAlternative = true;
        yield break;
      }

      var doorwayGroups = mainRoom.Prefab.GetComponentInChildren<MainRoomDoorwayGroups>();

      // index of MaxValue is how we tell which doorway proxy is fake
      var fakeDoorwayProxy = new DoorwayProxy(mainRoom, int.MaxValue, mainRoom.doorways[0].DoorwayComponent, Vector3.zero, Quaternion.identity);

      // nodes
      var nodesSorted = gen.DungeonFlow.Nodes.OrderBy(n => n.Position).ToList();
      var startingNodeIndexCache = -1;
      if (Properties.MainPathCopyNodeBehaviour == DunGenExtenderProperties.CopyNodeBehaviour.CopyFromNodeList) {
        startingNodeIndexCache = nodesSorted.FindIndex(n => n.TileSets.SelectMany(t => t.TileWeights.Weights).Any(t => t.Value == Properties.MainRoomTilePrefab));

        if (startingNodeIndexCache == -1) {
          Plugin.logger.LogWarning($"Switching to default dungeon branch generation due to CopyNodeBehaviour being CopyFromNodeList AND MainRoomTilePrefab not existing in the Nodes' tilesets");
          ActiveAlternative = false;
          yield return gen.Wait(gen.GenerateBranchPaths());
          ActiveAlternative = true;
          yield break;
        }

        startingNodeIndexCache++;
      }

      //FixDoorwaysToAllFloors(mainRoom, doorwayGroups);

      gen.ChangeStatus(GenerationStatus.MainPath);

			for (var b = 0; b < altCount; ++b) {
        RandomizeLineArchetypes(gen, true);
        var previousTile = mainRoom;
        var targetLength = Mathf.RoundToInt(gen.DungeonFlow.Length.GetRandom(gen.RandomStream) * gen.LengthMultiplier);
        var archetypes = new List<DungeonArchetype>(targetLength);

        var newMainPathTiles = new List<TileProxy>();
        newMainPathTiles.Add(mainRoom);

        int startingNodeIndex;
        if (Properties.MainPathCopyNodeBehaviour == DunGenExtenderProperties.CopyNodeBehaviour.CopyFromMainPathPosition) {
          var lineDepthRatio = Mathf.Clamp01(1f / (targetLength - 1));
          startingNodeIndex = nodesSorted.FindIndex(n => n.Position >= lineDepthRatio);
        } else if (Properties.MainPathCopyNodeBehaviour == DunGenExtenderProperties.CopyNodeBehaviour.CopyFromNodeList) {
          startingNodeIndex = startingNodeIndexCache;
        } else {
          Plugin.logger.LogError($"{Properties.MainPathCopyNodeBehaviour} is not yet defined. How did this happen?");
          startingNodeIndex = -1;
        }

        var nodes = nodesSorted.Skip(startingNodeIndex);
        var nodesVisited = new List<GraphNode>(nodes.Count());

        // most of this code is a mix of the GenerateMainPath()
        // and GenerateBranch() code
        for(var t = 1; t < targetLength; ++t){
          var lineDepthRatio = Mathf.Clamp01((float)t / (targetLength - 1));
          var lineAtDepth = gen.DungeonFlow.GetLineAtDepth(lineDepthRatio);
          if (lineAtDepth == null){
            yield return gen.Wait(gen.InnerGenerate(true));
            yield break;
          }

          if (lineAtDepth != gen.previousLineSegment){
            gen.currentArchetype = lineAtDepth.GetRandomArchetype(gen.RandomStream, archetypes);
            gen.previousLineSegment = lineAtDepth;
          }

          // terrible solution but FUCK it
          // and yet it worked
          // this is how my last node cannot be a target of pruning
          GraphNode graphNode = null;
          DungeonArchetype archetype = null;
          foreach(var g in nodes) {
            if (lineDepthRatio >= g.Position && !nodesVisited.Contains(g)) {
              graphNode = g;
              nodesVisited.Add(g);
              break;
            }
          }

          List<TileSet> useableTileSets;
          if (graphNode != null) {
            archetype = ModifyMainBranchNodeArchetype(null, graphNode, gen.RandomStream);
            useableTileSets = graphNode.TileSets;
          } else {
            archetype = gen.currentArchetype;
            useableTileSets = archetype.TileSets;
          }

          // places fake doorways at the first node
          if (doorwayGroups && t == 1){
            foreach(var d in mainRoom.UsedDoorways) {
              if (d.ConnectedDoorway.Index != int.MaxValue) {
                var groups = doorwayGroups.GrabDoorwayGroup(d.DoorwayComponent);
                if (groups == null) continue;

                foreach(var doorway in mainRoom.UnusedDoorways){
                  if (groups.Contains(doorway.DoorwayComponent)){
                    doorway.ConnectedDoorway = fakeDoorwayProxy;
                  }
                }
              }
            }
          }

          var tileProxy = gen.AddTile(previousTile, useableTileSets, lineDepthRatio, archetype, TilePlacementResult.None);
          
          if (tileProxy == null) {
            Plugin.logger.LogDebug($"Alt. main branch gen failed at {b}:{lineDepthRatio}");
            yield return gen.Wait(gen.InnerGenerate(true));
						yield break;
          }

          if (lineDepthRatio >= 1f){
            Plugin.logger.LogDebug($"Alt. main branch at {b} ended with {tileProxy.PrefabTile.name}");
          }

					tileProxy.Placement.BranchDepth = t;
					tileProxy.Placement.NormalizedBranchDepth = lineDepthRatio;

          if (graphNode != null) {
            tileProxy.Placement.GraphNode = graphNode;
					  tileProxy.Placement.GraphLine = null;
          } else {
            tileProxy.Placement.GraphNode = null;
					  tileProxy.Placement.GraphLine = lineAtDepth;
          }
					
					previousTile = tileProxy;
          newMainPathTiles.Add(tileProxy);

					if (gen.ShouldSkipFrame(true)) yield return gen.GetRoomPause();
        }

        allMainPathTiles.Add(newMainPathTiles);

			}

      // okay lets fix the fakes
      foreach(var doorway in mainRoom.UsedDoorways){
        if (doorway.ConnectedDoorway.Index == int.MaxValue) {
          doorway.ConnectedDoorway = null;
        }
      }

      ActiveAlternative = false;
      Plugin.logger.LogDebug($"Created {altCount} alt. paths, creating branches now");
      gen.ChangeStatus(GenerationStatus.Branching);

      // this is major trickery and it works still
      for(var b = 0; b < altCount + 1; ++b){
        Plugin.logger.LogDebug($"Branch {b}");
        RandomizeLineArchetypes(gen, false);
        gen.proxyDungeon.MainPathTiles = allMainPathTiles[b];
        yield return gen.Wait(gen.GenerateBranchPaths());
      }

      ActiveAlternative = true;

      gen.proxyDungeon.MainPathTiles = allMainPathTiles[0];

      AddForcedTiles(gen);
		}

    public static void AddForcedTiles(DungeonGenerator gen){
      if (!Properties.UseForcedTiles) return;

      var forcedTileSetLists = Properties.ForcedTileSets.ToList();
      while(forcedTileSetLists.Count > 0){
        var item = forcedTileSetLists[forcedTileSetLists.Count - 1];
        
        // we have to check ALL tiles
        var allTiles = gen.proxyDungeon.AllTiles
          .Select(t => {
            var depthValue = item.DepthWeightScale.Evaluate(t.Placement.NormalizedDepth);
            var weight = t.Placement.IsOnMainPath ? item.MainPathWeight : item.BranchPathWeight;
            return (t, depthValue * weight * gen.RandomStream.NextDouble());
          })
          .Where(pair => pair.Item2 > 0f)
          .OrderBy(pair => pair.Item2);

        // try every tile, if we somehow fail than man that sucks
        foreach(var pair in allTiles){
          var t = pair.t;
          var tileProxy = gen.AddTile(t, item.Tilesets, t.Placement.NormalizedDepth, null, TilePlacementResult.None);
          if (tileProxy == null) continue;

          tileProxy.Placement.BranchDepth = t.Placement.BranchDepth;
					tileProxy.Placement.NormalizedBranchDepth = t.Placement.NormalizedDepth;
          tileProxy.Placement.GraphNode = t.Placement.GraphNode;
					tileProxy.Placement.GraphLine = t.Placement.GraphLine;

          Plugin.logger.LogDebug($"Forcefully added tile {tileProxy.Prefab.name}");
          break;
        }

        forcedTileSetLists.RemoveAt(forcedTileSetLists.Count - 1);
      }

      Plugin.logger.LogDebug($"Forcefully added all tiles");
    }

    public static (TilePlacementResult result, TileProxy tile) ProcessDoorwayPairs(DungeonGenerator gen, DungeonArchetype archetype, Queue<DoorwayPair> doorwayPairs) {
      if (Properties != null && Properties.UseBranchLoopBoost && gen.Status == GenerationStatus.Branching) {
        return EncourageBranchPathLoopEncouragement(gen, doorwayPairs);
      } 

      while(doorwayPairs.Count > 0) {
        var pair = doorwayPairs.Dequeue();
        var result = gen.TryPlaceTile(pair, archetype, out var tileProxy);
        if (result == TilePlacementResult.None) return (result, tileProxy);
        gen.AddTilePlacementResult(result);
      }

      return (TilePlacementResult.NoValidTile, null);
    }

    public static (TilePlacementResult result, TileProxy tile) EncourageBranchPathLoopEncouragement(DungeonGenerator gen, Queue<DoorwayPair> doorwayPairs) {
      // get list of 5 potential targets
      var validTiles = new List<TilePlacementResultProxy>();
      while(doorwayPairs.Count > 0) {
        var pair = doorwayPairs.Dequeue();
        var value = GetTileProxyOfDoorwayPair(gen, pair);
        if (value.result == TilePlacementResult.None) {
          validTiles.Add(value);
          if (validTiles.Count >= Properties.BranchLoopBoostTileSearch) break;
        }
      }

      if (validTiles.Count == 0) {
        return (TilePlacementResult.NoValidTile, null);
      }

      // update their weight based on their potential doorway partners
      var allDungeonDoorways = gen.proxyDungeon.AllTiles.SelectMany(t => t.Doorways);
      //Plugin.logger.LogInfo("NEW TILES");
      foreach(var t in validTiles) {
        var doorwayCount = 0;
        foreach(var d in allDungeonDoorways) {
          foreach(var l in t.tile.doorways) {
            if (d.TileProxy == t.previousDoorway.TileProxy) continue;
            if (gen.DungeonFlow.CanDoorwaysConnect(d.TileProxy.PrefabTile, l.TileProxy.PrefabTile, d.DoorwayComponent, l.DoorwayComponent) && Vector3.SqrMagnitude(d.Position - l.Position) < 1E-05)
              doorwayCount++;
          }
        }

        if (doorwayCount > 0) {
          //Plugin.logger.LogInfo($"{t.weight} -> {t.weight * (1f + doorwayCount * Properties.BranchLoopBoostTileScale)} ({doorwayCount})");
          t.weight *= (1f + doorwayCount * Properties.BranchLoopBoostTileScale);
        } else {
          //Plugin.logger.LogInfo($"{t.weight}");
        }
      }

      var bestChoice = validTiles.OrderByDescending(t => t.weight).FirstOrDefault();
      //Plugin.logger.LogInfo($"Best: {bestChoice.weight}");

      MakeTileProxyConnection(gen, bestChoice);
      gen.AddTilePlacementResult(bestChoice.result);

      return (bestChoice.result, bestChoice.tile);
    }

    private class TilePlacementResultProxy {
      public TilePlacementResult result;
      public TileProxy tile;
      public DoorwayProxy previousDoorway;
      public DoorwayProxy nextDoorway;
      public float weight;

      public TilePlacementResultProxy(TilePlacementResult result) {
        this.result = result;
        tile = null;
        previousDoorway = null;
        nextDoorway = null;
        weight = 0f;
      }

      public TilePlacementResultProxy(TilePlacementResult result, TileProxy tile, DoorwayProxy previousDoorway, DoorwayProxy nextDoorway, float weight) {
        this.result = result;
        this.tile = tile;
        this.previousDoorway = previousDoorway;
        this.nextDoorway = nextDoorway;
        this.weight = weight;
      }

    }

    private static TilePlacementResultProxy GetTileProxyOfDoorwayPair(DungeonGenerator gen, DoorwayPair pair){
      var nextTemplate = pair.NextTemplate;
      var previousDoorway = pair.PreviousDoorway;
      if (nextTemplate == null) return new TilePlacementResultProxy(TilePlacementResult.TemplateIsNull);

      var index = pair.NextTemplate.Doorways.IndexOf(pair.NextDoorway);
      var tile = new TileProxy(nextTemplate);
      tile.Placement.isOnMainPath = false;
      tile.Placement.TileSet = pair.NextTileSet;

      if (previousDoorway != null) {
        var myDoorway = tile.Doorways[index];
        tile.PositionBySocket(myDoorway, previousDoorway);
        var bounds = tile.Placement.Bounds;
        if (gen.RestrictDungeonToBounds && !gen.TilePlacementBounds.Contains(bounds)) return new TilePlacementResultProxy(TilePlacementResult.OutOfBounds);
        if (gen.IsCollidingWithAnyTile(tile, previousDoorway.TileProxy)) return new TilePlacementResultProxy(TilePlacementResult.TileIsColliding);
      }

      if (tile == null) return new TilePlacementResultProxy(TilePlacementResult.NewTileIsNull);

      tile.Placement.PathDepth = pair.PreviousTile.Placement.PathDepth;
      tile.Placement.BranchDepth = pair.PreviousTile.Placement.IsOnMainPath ? 0 : (pair.PreviousTile.Placement.BranchDepth + 1);

      return new TilePlacementResultProxy(TilePlacementResult.None, tile, previousDoorway, tile.Doorways[index], pair.TileWeight);
    }

    private static void MakeTileProxyConnection(DungeonGenerator gen, TilePlacementResultProxy proxy) {
      if (proxy.previousDoorway != null) {
        gen.proxyDungeon.MakeConnection(proxy.previousDoorway, proxy.nextDoorway);
      }
      gen.proxyDungeon.AddTile(proxy.tile);
    }

    public static void RandomizeLineArchetypes(DungeonGenerator gen, bool randomizeMainPath){
      if (!Properties.UseLineRandomizer) return;  

      var flow = Instance.DungeonFlow;
      var lines = flow.Lines;
      var tilesetsUsed = new Dictionary<TileSet, int>();
      foreach(var t in Properties.LineRandomizerTileSets){
        tilesetsUsed.Add(t, 0);
      }

      foreach(var a in Properties.LineRandomizerArchetypes) {
        var tiles = randomizeMainPath ? a.TileSets : a.BranchCapTileSets;
        RandomizeArchetype(gen, tiles, tilesetsUsed);
      }
    }

    public static void RandomizeArchetype(DungeonGenerator gen, List<TileSet> targetTileSet, Dictionary<TileSet, int> tilesetsUsed){
      // get 3 random
      var newTiles = Properties.LineRandomizerTileSets
        .OrderBy(t => tilesetsUsed[t] + gen.RandomStream.NextDouble())
        .Take(Properties.LineRandomizerTakeCount);

      var i = targetTileSet.Count - 1;
      foreach(var n in newTiles){
        targetTileSet[i] = n;
        --i;

        tilesetsUsed[n] += 1;
      }
    }

    public static DungeonArchetype ModifyMainBranchNodeArchetype(DungeonArchetype archetype, GraphNode node, RandomStream randomStream){
      if (!DunGenPlusGenerator.Active) return archetype;
      
      if (Properties.AddArchetypesToNormalNodes && node.NodeType == NodeType.Normal) {
        return Properties.GetRandomArchetype(node.Label, randomStream);;
      }
      return archetype;
    }

    public static void FixDoorwaysToAllFloors(TileProxy mainRoom, MainRoomDoorwayGroups doorwayGroups) {
      var first = doorwayGroups.doorwayListFirst;
      if (first == null) return;

      foreach(var target in mainRoom.UsedDoorways){
        if (target.ConnectedDoorway.Index == int.MaxValue && !first.Contains(target.DoorwayComponent)) {
          target.ConnectedDoorway = null;
        }
      }
    }

    /*
    public static GraphNode ModifyGraphNode(GraphNode node) {
      if (!Patch.active) return node;

      if (node.Label == "Hallway Entrance 1") {
        return Assets.networkObjectList.gardenEntranceGraphNode;
      }
      return node;
    }

    public static TileProxy FixTilesToAllFloors(TileProxy mainTile) {
      if (!Patch.active) return mainTile;

      var groups = mainTile.Prefab.GetComponentInChildren<MainRoomDoorwayGroups>();
      var first = groups.groupFirst;

      foreach(var target in mainTile.doorways){
        if (target.ConnectedDoorway != null && target.ConnectedDoorway.Index == int.MaxValue && !first.Contains(target.DoorwayComponent)) {
          target.ConnectedDoorway = null;
        }
      }

      return mainTile;
    }
    */

  }
}
