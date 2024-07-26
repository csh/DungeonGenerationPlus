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
        generator.RestrictDungeonToBounds = Properties.UseDungeonBounds;
        var bounds = Properties.GetDungeonBounds(generator.LengthMultiplier);
        generator.TilePlacementBounds = bounds;
        Plugin.logger.LogInfo($"Dungeon Bounds: {bounds}");
      }

      if (Properties.UseMaxShadowsRequestUpdate) {
        Plugin.logger.LogInfo($"Updating HDRP asset: setting max shadows request to {Properties.MaxShadowsRequestAmount}");
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
        Plugin.logger.LogInfo("Restoring original HDRP asset");

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
      var startingNodeIndex = nodesSorted.FindIndex(n => n.TileSets.SelectMany(t => t.TileWeights.Weights).Any(t => t.Value == Properties.MainRoomTilePrefab));
      if (startingNodeIndex == -1) {
        Plugin.logger.LogWarning($"Switching to default dungeon branch generation due to MainRoomTilePrefab not existing in the Nodes' tilesets");
        ActiveAlternative = false;
        yield return gen.Wait(gen.GenerateBranchPaths());
        ActiveAlternative = true;
        yield break;
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

        var nodes = nodesSorted.Skip(startingNodeIndex + 1);
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
            Plugin.logger.LogInfo($"Alt. main branch gen failed at {b}:{lineDepthRatio}");
            yield return gen.Wait(gen.InnerGenerate(true));
						yield break;
          }

          if (lineDepthRatio >= 1f){
            Plugin.logger.LogInfo($"Alt. main branch at {b} ended with {tileProxy.PrefabTile.name}");
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
      Plugin.logger.LogInfo($"Created {altCount} alt. paths, creating branches now");
      gen.ChangeStatus(GenerationStatus.Branching);

      // this is major trickery and it works still
      for(var b = 0; b < altCount + 1; ++b){
        Plugin.logger.LogInfo($"Branch {b}");
        RandomizeLineArchetypes(gen, false);
        gen.proxyDungeon.MainPathTiles = allMainPathTiles[b];
        yield return gen.Wait(gen.GenerateBranchPaths());
      }

      ActiveAlternative = true;

      gen.proxyDungeon.MainPathTiles = allMainPathTiles[0];
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
