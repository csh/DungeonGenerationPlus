using DunGen;
using DunGen.Graph;
using DunGenPlus.DevTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

  }
}
