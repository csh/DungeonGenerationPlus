using DunGen;
using DunGen.Graph;
using DunGenPlus.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.DevTools.Panels.Collections {
  internal class DungeonFlowCacheAssets {
    public DunGenExtenderProperties originalProperties;

    // Albino said that readonly is safer
    public struct Collection<T> {
      public ReadOnlyCollection<T> list;
      public ReadOnlyDictionary<T, int> dictionary;
      public ReadOnlyCollection<string> options;

      public Collection(List<T> list) {
        this.list = new ReadOnlyCollection<T>(list);

        var tempDictionary = new Dictionary<T, int>();  
        for(var i = 0;  i < list.Count; i++) {
          tempDictionary.Add(list[i], i);
        }
        dictionary = new ReadOnlyDictionary<T, int>(tempDictionary);

        options = new ReadOnlyCollection<string>(list.Select(l => l.ToString()).ToList());
      }
    }

    public readonly Collection<NullObject<TileSet>> tileSets;
    public readonly Collection<NullObject<GameObject>> tiles;
    public readonly Collection<NullObject<DungeonArchetype>> archetypes;

    public DungeonFlowCacheAssets(DungeonFlow dungeonFlow, DunGenExtender extender){
      if (extender){
        originalProperties = extender.Properties.Copy();
      }
      
      var tileSetsHashSet = new HashSet<NullObject<TileSet>>() { new NullObject<TileSet>(null) };
      var tilesHashSet = new HashSet<NullObject<GameObject>>() { new NullObject<GameObject>(null) };
      var archetypesHashSet = new HashSet<NullObject<DungeonArchetype>>() { new NullObject<DungeonArchetype>(null) };

      foreach(var t in dungeonFlow.Nodes) {
        var label = t.Label.ToLowerInvariant();
        if (label == "lchc gate" || label == "goal"){
          foreach(var n in t.TileSets.SelectMany(x => x.TileWeights.Weights)) {
            n.Value.GetComponent<Tile>().RepeatMode = TileRepeatMode.Allow;
          }
        }

      }

      void AddTiles(IEnumerable<GameObject> tiles){
        foreach(var x in tiles) {
          tilesHashSet.Add(x);
        }
      }

      void AddTilesW(IEnumerable<GameObjectChance> tiles){
        foreach(var x in tiles) {
          tilesHashSet.Add(x.Value);
        }
      }

      void AddTileSets(IEnumerable<TileSet> tileSets){
        foreach(var x in tileSets){
          tileSetsHashSet.Add(x);
          AddTilesW(x.TileWeights.Weights);
        }
      }

      void AddArchetypes(IEnumerable<DungeonArchetype> archetypes){
        foreach(var x in archetypes){
          archetypesHashSet.Add(x);
          AddTileSets(x.TileSets);
        }
      }

      AddTileSets(dungeonFlow.Nodes.SelectMany(n => n.TileSets));
      AddArchetypes(dungeonFlow.Lines.SelectMany(l => l.DungeonArchetypes));
      AddTileSets(dungeonFlow.TileInjectionRules.Select(n => n.TileSet));

      if (extender) {
        AddArchetypes(extender.Properties.NormalNodeArchetypesProperties.NormalNodeArchetypes.SelectMany(l => l.Archetypes));
        AddTileSets(extender.Properties.ForcedTilesProperties.ForcedTileSets.SelectMany(l => l.TileSets));

        AddTiles(extender.Properties.AssetCacheTileList);
        AddTileSets(extender.Properties.AssetCacheTileSetList);
        AddArchetypes(extender.Properties.AssetCacheArchetypeList);
      }

      tileSets = new Collection<NullObject<TileSet>>(tileSetsHashSet.ToList());
      tiles = new Collection<NullObject<GameObject>>(tilesHashSet.ToList());
      archetypes = new Collection<NullObject<DungeonArchetype>>(archetypesHashSet.ToList());
    }
  }
}
