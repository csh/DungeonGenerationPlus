using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DunGen;
using DunGen.Graph;
using DunGenPlus;
using DunGenPlus.Collections;
using HarmonyLib;

namespace LoadstoneNighty {

  public class Patch {

    public static void Activate(){
      Loadstone.Patches.DungenOptimizationPatches.tileCollectors.Add(GetTiles);
    }

    private static HashSet<Tile> GetTiles(DungeonGenerator generator) {
      var flow = generator.DungeonFlow;
      var extender = API.GetDunGenExtender(flow);
      var hashset = new HashSet<Tile>();
      
      if (API.IsDunGenExtenderActive(extender)){
        Plugin.logger.LogDebug("Creating custom hashset for Loadstone");
        var props = extender.Properties;
        GenerateTileHashSet(ref hashset, props.MainPathProperties.MainPathDetails);
        GenerateTileHashSet(ref hashset, props.AdditionalTilesProperties.AdditionalTileSets);
        GenerateTileHashSet(ref hashset, props.NormalNodeArchetypesProperties.NormalNodeArchetypes);
        GenerateTileHashSet(ref hashset, props.LineRandomizerProperties.Archetypes);
      }
      return hashset;
    }

    static void GenerateTileHashSet(ref HashSet<Tile> tiles, List<NodeArchetype> nodes) {
		  foreach (var n in nodes) {
        GenerateTileHashSet(ref tiles, n.Archetypes);
		  }
	  }

    static void GenerateTileHashSet(ref HashSet<Tile> tiles, List<AdditionalTileSetList> list) {
		  foreach (var l in list) {
        GenerateTileHashSet(ref tiles, l.TileSets);
		  }
	  }

    static void GenerateTileHashSet(ref HashSet<Tile> tiles, List<MainPathExtender> extenders) {
		  foreach (var ext in extenders) {
        GenerateTileHashSet(ref tiles, ext.Nodes.Value);
        GenerateTileHashSet(ref tiles, ext.Lines.Value);
		  }
	  }

    static void GenerateTileHashSet(ref HashSet<Tile> tiles, List<GraphNode> nodes) {
		  foreach (var n in nodes) {
        GenerateTileHashSet(ref tiles, n.TileSets);
		  }
	  }

    static void GenerateTileHashSet(ref HashSet<Tile> tiles, List<GraphLine> lines) {
		  foreach (var l in lines) {
        GenerateTileHashSet(ref tiles, l.DungeonArchetypes);
		  }
	  }

    static void GenerateTileHashSet(ref HashSet<Tile> tiles, List<DungeonArchetype> archetypes) {
		  foreach (var a in archetypes) {
        GenerateTileHashSet(ref tiles, a.TileSets);
				GenerateTileHashSet(ref tiles, a.BranchCapTileSets);
		  }
	  }

    static void GenerateTileHashSet(ref HashSet<Tile> tiles, List<TileSet> tileSets) {
		  foreach (var tileSet in tileSets) {
			  foreach (var tileChance in tileSet.TileWeights.Weights) {
				  var tile = tileChance.Value.GetComponent<Tile>();
				  if (tile != null) tiles.Add(tile);
			  }
		  }
	  }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Loadstone.Patches.FromProxyPatches), "FromProxyEnd")]
    public static void FromProxyEndPatch(Dictionary<TileProxy, Tile> dictionary){
      DunGenPlus.API.AddTileToMainPathDictionary(dictionary);
    }

  }
}
