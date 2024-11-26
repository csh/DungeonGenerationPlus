using DunGen;
using DunGen.Graph;
using DunGenPlus.DevTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace DunGenPlus.Generation {
  internal partial class DunGenPlusGenerator {

    public static void AddForcedTiles(DungeonGenerator gen){
      if (!Properties.AdditionalTilesProperties.UseAdditionalTiles) return;

      var forcedTileSetLists = Properties.AdditionalTilesProperties.AdditionalTileSets.ToList();
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
          var tileProxy = gen.AddTile(t, item.TileSets, t.Placement.NormalizedDepth, null, TilePlacementResult.None);
          if (tileProxy == null) continue;

          AddTileProxy(tileProxy, GetMainPathIndexFromTileProxy(t));
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

    public static void RandomizeLineArchetypes(DungeonGenerator gen, bool randomizeMainPath){
      if (!Properties.LineRandomizerProperties.UseLineRandomizer) return;  

      var flow = Instance.DungeonFlow;
      var lines = flow.Lines;
      var tilesetsUsed = new Dictionary<TileSet, int>();
      foreach(var t in Properties.LineRandomizerProperties.TileSets){
        tilesetsUsed.Add(t, 0);
      }

      foreach(var a in Properties.LineRandomizerProperties.Archetypes) {
        var tiles = randomizeMainPath ? a.TileSets : a.BranchCapTileSets;
        RandomizeArchetype(gen, tiles, tilesetsUsed);
      }
    }

    public static void RandomizeArchetype(DungeonGenerator gen, List<TileSet> targetTileSet, Dictionary<TileSet, int> tilesetsUsed){
      // get 3 random
      var newTiles = Properties.LineRandomizerProperties.TileSets
        .OrderBy(t => tilesetsUsed[t] + gen.RandomStream.NextDouble())
        .Take(Properties.LineRandomizerProperties.TileSetsTakeCount);

      var i = targetTileSet.Count - 1;
      foreach(var n in newTiles){
        targetTileSet[i] = n;
        --i;

        tilesetsUsed[n] += 1;
      }
    }

    public static DungeonArchetype ModifyMainBranchNodeArchetype(DungeonArchetype archetype, GraphNode node, RandomStream randomStream){
      if (!DunGenPlusGenerator.Active) return archetype;
      
      if (Properties.NormalNodeArchetypesProperties.AddArchetypesToNormalNodes && node.NodeType == NodeType.Normal) {
        return Properties.NormalNodeArchetypesProperties.GetRandomArchetype(node.Label, randomStream);;
      }
      return archetype;
    }

    public static bool AllowRetryStop(bool defaultState){
      return defaultState || DevDebugManager.IsActive;
    }

    // Copied and pasted from DunGen
    public static void ProcessGlobalPropsPerMainPath(DungeonGenerator dungeonGenerator){
      var localIDs = Properties.MainPathProperties.LocalMainPathGlobalProps.Select(x => x.ID).ToHashSet();

      // first parameter int is the GlobalProp ID
      // second parameter is the List of GameObjectChanceTable indexed by the main path index
      var localDictionary = new Dictionary<int, Dictionary<int, GameObjectChanceTable>>();

      // default dictionary
      var globalDictionary = new Dictionary<int, GameObjectChanceTable>();

      foreach(var tile in dungeonGenerator.CurrentDungeon.AllTiles){
        foreach(var globalProp in tile.GetComponentsInChildren<GlobalProp>()){
          GameObjectChanceTable gameObjectChanceTable;
          if (localIDs.Contains(globalProp.PropGroupID)){
            if (!localDictionary.TryGetValue(globalProp.PropGroupID, out var dictionary)){
              dictionary = new Dictionary<int, GameObjectChanceTable>();
              localDictionary.Add(globalProp.PropGroupID, dictionary);
            }

            // The game will softlock if it can't find the tile
            // THIS IS SCARY!!!
            var mainPathIndex = GetMainPathIndexFromTile(tile);
            if (!dictionary.TryGetValue(mainPathIndex, out gameObjectChanceTable)){
              gameObjectChanceTable = new GameObjectChanceTable();
              dictionary.Add(mainPathIndex, gameObjectChanceTable);
            }
          } else {
            if (!globalDictionary.TryGetValue(globalProp.PropGroupID, out gameObjectChanceTable)){
              gameObjectChanceTable = new GameObjectChanceTable();
              globalDictionary.Add(globalProp.PropGroupID, gameObjectChanceTable);
            }
          }

          var num = tile.Placement.IsOnMainPath ? globalProp.MainPathWeight : globalProp.BranchPathWeight;
          num *= globalProp.DepthWeightScale.Evaluate(tile.Placement.NormalizedDepth);
          gameObjectChanceTable.Weights.Add(new GameObjectChance(globalProp.gameObject, num, 0f, null));

        }
      }

      foreach(var dictionary in localDictionary.Values){
        foreach(var table2 in dictionary.Values){
          foreach(var gameObjectChance in table2.Weights){
            gameObjectChance.Value.SetActive(false);
          }
        }
      }

      foreach(var table2 in globalDictionary.Values){
        foreach(var gameObjectChance in table2.Weights){
          gameObjectChance.Value.SetActive(false);
        }
      }

      var list = new List<int>(localDictionary.Count + globalDictionary.Count);

      int ProcessGlobalPropID(GameObjectChanceTable table, int localMax, int globalCount, int globalMax){
        localMax = Mathf.Clamp(localMax, 0, table.Weights.Count);
        var i = 0;
        while(i < localMax && i + globalCount < globalMax){
          var random = table.GetRandom(dungeonGenerator.RandomStream, true, 0f, null, true, true);
          if (random != null && random.Value != null) {
            random.Value.SetActive(true);
          }
          ++i;
        }
        return i;
      }

      using(var enumerator = globalDictionary.GetEnumerator()){
        while(enumerator.MoveNext()){
          var pair = enumerator.Current;
          if (list.Contains(pair.Key)){
            Plugin.logger.LogWarning("Dungeon Flow contains multiple entries for the global prop group ID: " + pair.Key.ToString() + ". Only the first entry will be used.");
          } else {
            //Plugin.logger.LogWarning($"{pair.Key}: Global");

            var globalPropSettings = dungeonGenerator.DungeonFlow.GlobalProps
              .Where(x => x.ID == pair.Key)
              .FirstOrDefault();

            if (globalPropSettings != null){
              var tableClone = pair.Value.Clone();
              var globalMax = globalPropSettings.Count.GetRandom(dungeonGenerator.RandomStream);

              var spawned = ProcessGlobalPropID(tableClone, globalMax, 0, globalMax);
              //Plugin.logger.LogError($"Spawned {spawned} ({spawned}/{globalMax})");
              list.Add(pair.Key);
            }
          }
        }
      }

      using(var enumerator = localDictionary.GetEnumerator()){
        while(enumerator.MoveNext()){
          var pair = enumerator.Current;
          if (list.Contains(pair.Key)){
            Plugin.logger.LogWarning("Dungeon Flow contains multiple entries for the global prop group ID: " + pair.Key.ToString() + ". Only the first entry will be used.");
          } else {
            //Plugin.logger.LogWarning($"{pair.Key}: Local");

            var globalPropSettings = dungeonGenerator.DungeonFlow.GlobalProps
              .Where(x => x.ID == pair.Key)
              .FirstOrDefault();
            var localPropSettings = Properties.MainPathProperties.LocalMainPathGlobalProps
              .Where(x => x.ID == pair.Key)
              .FirstOrDefault();

            if (globalPropSettings != null && localPropSettings != null){
              var globalCount = 0;
              foreach(var path in pair.Value.Values){
                var tableClone = path.Clone();
                var globalMax = globalPropSettings.Count.GetRandom(dungeonGenerator.RandomStream);
                var localMax = localPropSettings.Count.GetRandom(dungeonGenerator.RandomStream);

                var spawned =  ProcessGlobalPropID(tableClone, localMax, globalCount, globalMax);
                globalCount += spawned;
                //Plugin.logger.LogError($"Spawned {spawned} ({globalCount}/{globalMax})");
              }
              list.Add(pair.Key);
            }
          }
        }
      }

    }

  }
}
