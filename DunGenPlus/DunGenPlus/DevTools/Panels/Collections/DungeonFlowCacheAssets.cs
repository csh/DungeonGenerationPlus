using DunGen;
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

    public DungeonFlowCacheAssets(DunGenExtender extender){
      originalProperties = extender.Properties.Copy();
        
      var tileSetsHashSet = new HashSet<NullObject<TileSet>>() { new NullObject<TileSet>(null) };
      var tilesHashSet = new HashSet<NullObject<GameObject>>() { new NullObject<GameObject>(null) };
      var archetypesHashSet = new HashSet<NullObject<DungeonArchetype>>() { new NullObject<DungeonArchetype>(null) };

      foreach(var t in extender.DungeonFlow.Nodes) {
        var label = t.Label.ToLowerInvariant();
        if (label == "lchc gate" || label == "goal"){
          foreach(var n in t.TileSets.SelectMany(x => x.TileWeights.Weights)) {
            n.Value.GetComponent<Tile>().RepeatMode = TileRepeatMode.Allow;
          }
        }

      }

      foreach(var t in extender.DungeonFlow.Nodes.SelectMany(n => n.TileSets)) {
        tileSetsHashSet.Add(t);
        foreach(var x in t.TileWeights.Weights) {
          tilesHashSet.Add(x.Value);
        }
      }
      foreach(var a in extender.DungeonFlow.Lines.SelectMany(l => l.DungeonArchetypes)) {
        archetypesHashSet.Add(a);
        foreach(var t in a.TileSets) {
          tileSetsHashSet.Add(t);
          foreach(var x in t.TileWeights.Weights) {
            tilesHashSet.Add(x.Value);
          }
        }
      }

      foreach(var n in extender.Properties.NormalNodeArchetypes) {
        foreach(var a in n.archetypes){
          archetypesHashSet.Add(a);

          foreach(var t in a.TileSets){
            tileSetsHashSet.Add(t);
            foreach(var x in t.TileWeights.Weights){
              tilesHashSet.Add(x.Value);
            }
          }
        }
      }

      foreach(var t in extender.Properties.ForcedTileSets.SelectMany(l => l.Tilesets)){
        tileSetsHashSet.Add(t);
        foreach(var x in t.TileWeights.Weights){
          tilesHashSet.Add(x.Value);
        }
      }

      tileSets = new Collection<NullObject<TileSet>>(tileSetsHashSet.ToList());
      tiles = new Collection<NullObject<GameObject>>(tilesHashSet.ToList());
      archetypes = new Collection<NullObject<DungeonArchetype>>(archetypesHashSet.ToList());
    }
  }
}
