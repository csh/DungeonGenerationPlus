using DunGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Collections {
  [System.Serializable]
  public class ForcedTileSetList {

    [Tooltip("List of tiles to be forcefully spawned.")]
    public List<TileSet> Tilesets = new List<TileSet>();
    [Tooltip("The weight based on the path's depth.")]
    public AnimationCurve DepthWeightScale = new AnimationCurve();
    [Tooltip("The weight for the tile spawning on the main path.")]
    public float MainPathWeight = 1f;
    [Tooltip("The weight for the tile spawning on the branch path.")]
    public float BranchPathWeight = 1f;

  }
}
