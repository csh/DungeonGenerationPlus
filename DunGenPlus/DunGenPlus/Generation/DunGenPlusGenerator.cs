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
using DunGenPlus.DevTools;

[assembly: SecurityPermission( SecurityAction.RequestMinimum, SkipVerification = true )]
namespace DunGenPlus.Generation {
  internal partial class DunGenPlusGenerator {
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
      var callback = new EventCallbackScenario(DevDebugManager.Instance);
      Instance.Events.OnModifyDunGenExtenderProperties.Invoke(props, callback);
      props.NormalNodeArchetypesProperties.SetupProperties(generator);
      Properties = props;

      if (Properties.DungeonBoundsProperties.UseDungeonBounds) {
        generator.DebugRender = true;
        generator.RestrictDungeonToBounds = true;
        var bounds = Properties.DungeonBoundsProperties.GetDungeonBounds(generator.LengthMultiplier);
        generator.TilePlacementBounds = bounds;
        Plugin.logger.LogDebug($"Dungeon Bounds: {bounds}");
      }

      if (Properties.MiscellaneousProperties.UseMaxShadowsRequestUpdate) {
        Plugin.logger.LogDebug($"Updating HDRP asset: setting max shadows request to {Properties.MiscellaneousProperties.MaxShadowsRequestCount}");
        try {
          previousHDRPAsset = QualitySettings.renderPipeline as HDRenderPipelineAsset;
          newHDRPAsset = ScriptableObject.Instantiate(previousHDRPAsset);
      
          var settings = newHDRPAsset.currentPlatformRenderPipelineSettings;
          settings.hdShadowInitParams.maxScreenSpaceShadowSlots = Properties.MiscellaneousProperties.MaxShadowsRequestCount;
          newHDRPAsset.currentPlatformRenderPipelineSettings = settings;

          QualitySettings.renderPipeline = newHDRPAsset;
        } catch (Exception e) {
          Plugin.logger.LogError("Failed to update HDRP asset");
          Plugin.logger.LogError(e.ToString());
        }
      }


      DoorwayManager.ResetList();
      DoorwayManager.onMainEntranceTeleportSpawnedEvent.ClearTemporaryActionList();
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

    private static Dictionary<TileProxy, int> tileProxyMainPath = new Dictionary<TileProxy, int>();

    public static int GetMainPathIndexFromTileProxy(TileProxy tileProxy){
      return tileProxyMainPath[tileProxy];
    }

    private static void AddTileProxyToMainPathDictionary(IEnumerable<TileProxy> tileProxies, int index) {  
      var totalLength = (float)tileProxies.Last().Placement.PathDepth;
      foreach(var t in tileProxies) {
        tileProxyMainPath.Add(t, index);
        t.Placement.NormalizedPathDepth = t.Placement.PathDepth / totalLength;
      }
    }

    public static IEnumerator GenerateAlternativeMainPaths(DungeonGenerator gen) {
      // default behaviour
      if (!Active) {
        ActiveAlternative = false;
        yield return gen.Wait(gen.GenerateBranchPaths());
        ActiveAlternative = true;
        yield break;
      }

      var altCount = Properties.MainPathProperties.MainPathCount - 1;
      tileProxyMainPath.Clear();

      var mainRoomTilePrefab = Properties.MainPathProperties.MainRoomTilePrefab;
      var copyNodeBehaviour = Properties.MainPathProperties.CopyNodeBehaviour;

      if (altCount <= 0) {
        yield return gen.Wait(GenerateBranchPaths(gen, $"MainPathCount being {altCount + 1}", LogLevel.Info));
        yield break;
      }

      if (mainRoomTilePrefab == null) {
        yield return gen.Wait(GenerateBranchPaths(gen, $"MainRoomTilePrefab being null", LogLevel.Warning));
        yield break;
      }

      var allMainPathTiles = new List<List<TileProxy>>();
      var firstMainPathTiles = gen.proxyDungeon.MainPathTiles.ToList();
      allMainPathTiles.Add(firstMainPathTiles);
      AddTileProxyToMainPathDictionary(firstMainPathTiles, 0);

      // main room is the true main room and not the fake room
      // this MUST have multiple doorways as you can imagine
      var mainRoom = gen.proxyDungeon.MainPathTiles.FirstOrDefault(t => t.Prefab == mainRoomTilePrefab);
      if (mainRoom == null) {
        yield return gen.Wait(GenerateBranchPaths(gen, $"MainRoomTilePrefab not spawning on the main path", LogLevel.Warning));
        yield break;
      }

      var doorwayGroups = mainRoom.Prefab.GetComponentInChildren<MainRoomDoorwayGroups>();

      // index of MaxValue is how we tell which doorway proxy is fake
      var fakeDoorwayProxy = new DoorwayProxy(mainRoom, int.MaxValue, mainRoom.doorways[0].DoorwayComponent, Vector3.zero, Quaternion.identity);

      // nodes
      var nodesSorted = gen.DungeonFlow.Nodes.OrderBy(n => n.Position).ToList();
      var startingNodeIndexCache = -1;
      if (copyNodeBehaviour == DunGenExtenderProperties.CopyNodeBehaviour.CopyFromNodeList) {
        startingNodeIndexCache = nodesSorted.FindIndex(n => n.TileSets.SelectMany(t => t.TileWeights.Weights).Any(t => t.Value == mainRoomTilePrefab));

        if (startingNodeIndexCache == -1) {
          yield return gen.Wait(GenerateBranchPaths(gen, $"CopyNodeBehaviour being CopyFromNodeList AND MainRoomTilePrefab not existing in the Nodes' tilesets", LogLevel.Warning));
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
        // this causes the main room to create three sets of branch paths
        // newMainPathTiles.Add(mainRoom);

        int startingNodeIndex;
        if (copyNodeBehaviour == DunGenExtenderProperties.CopyNodeBehaviour.CopyFromMainPathPosition) {
          var lineDepthRatio = Mathf.Clamp01(1f / (targetLength - 1));
          startingNodeIndex = nodesSorted.FindIndex(n => n.Position >= lineDepthRatio);
        } else if (copyNodeBehaviour == DunGenExtenderProperties.CopyNodeBehaviour.CopyFromNodeList) {
          startingNodeIndex = startingNodeIndexCache;
        } else {
          Plugin.logger.LogError($"{copyNodeBehaviour} is not yet defined. How did this happen?");
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

        AddTileProxyToMainPathDictionary(newMainPathTiles, b + 1);
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

        if (Properties.BranchPathMultiSimulationProperties.UseBranchPathMultiSim) {
          GenerateBranchBoostedPathsStopWatch.Reset();
          GenerateBranchBoostedPathsStopWatch.Start();
          yield return gen.Wait(GenerateMultiBranchPaths(gen));
          GenerateBranchBoostedPathsStopWatch.Stop();
          GenerateBranchBoostedPathsTime += (float)GenerateBranchBoostedPathsStopWatch.Elapsed.TotalMilliseconds;
        }
        else yield return gen.Wait(gen.GenerateBranchPaths());
      }

      ActiveAlternative = true;

      gen.proxyDungeon.MainPathTiles = allMainPathTiles[0];

      AddForcedTiles(gen);
		}

    private static IEnumerator GenerateBranchPaths(DungeonGenerator gen, string message, LogLevel logLevel){
      Plugin.logger.Log(logLevel, $"Switching to default dungeon branch generation: {message}");

      ActiveAlternative = false;
      yield return gen.Wait(gen.GenerateBranchPaths());
      ActiveAlternative = true;

      AddForcedTiles(gen);
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
