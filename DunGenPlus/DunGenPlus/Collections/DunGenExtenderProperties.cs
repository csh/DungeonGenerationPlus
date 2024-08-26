using DunGen.Graph;
using DunGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Collections {

  [System.Serializable]
  public class DunGenExtenderProperties {

    public enum CopyNodeBehaviour {
      /// <summary>
      /// Nodes will copy from the MainRoomTilePrefab's position in the main path
      /// </summary>
      CopyFromMainPathPosition,
      /// <summary>
      /// Nodes will copy from the MainRoomTilePrefab's location from the node list + 1
      /// </summary>
      CopyFromNodeList
    }

    [Header("Main Path")]
    public MainPathProperties MainPathProperties = new MainPathProperties();
    
    [Header("Dungeon Bounds")]
    public DungeonBoundsProperties DungeonBoundsProperties = new DungeonBoundsProperties();

    [Header("Normal Nodes Archetypes")]
    public NormalNodeArchetypesProperties NormalNodeArchetypesProperties = new NormalNodeArchetypesProperties();

    [Header("Forced Tiles")]
    public ForcedTilesProperties ForcedTilesProperties = new ForcedTilesProperties();

    [Header("Branch Path Multi Simulation")]
    public BranchPathMultiSimulationProperties BranchPathMultiSimulationProperties = new BranchPathMultiSimulationProperties();

    [Header("Line Randomizer")]
    public LineRandomizerProperties LineRandomizerProperties = new LineRandomizerProperties();

    [Header("Miscellaneous")]
    public MiscellaneousProperties MiscellaneousProperties = new MiscellaneousProperties();

    [Header("Asset Cache (FOR DEV DEBUG PURPOSES ONLY)")]
    public List<GameObject> AssetCacheTileList = new List<GameObject>();
    public List<TileSet> AssetCacheTileSetList = new List<TileSet>();
    public List<DungeonArchetype> AssetCacheArchetypeList = new List<DungeonArchetype>();

    internal void CopyFrom(DunGenExtenderProperties props) {
      MainPathProperties = props.MainPathProperties.Copy();
      DungeonBoundsProperties = props.DungeonBoundsProperties.Copy();
      NormalNodeArchetypesProperties = props.NormalNodeArchetypesProperties.Copy();
      ForcedTilesProperties = props.ForcedTilesProperties.Copy();
      BranchPathMultiSimulationProperties = props.BranchPathMultiSimulationProperties.Copy();
      LineRandomizerProperties = props.LineRandomizerProperties.Copy();
      MiscellaneousProperties = props.MiscellaneousProperties.Copy();
    }

    internal DunGenExtenderProperties Copy() {
      var copy = new DunGenExtenderProperties();
      copy.CopyFrom(this);
      return copy;
    }

  }
}
