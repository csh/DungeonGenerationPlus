using DunGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Generation {
  internal partial class DunGenPlusGenerator {

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

  }
}
