using DunGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Collections {

  [System.Serializable]
  public class DetailedGlobalPropSettings {

    internal const string MinimumDistanceTooltip = "The minimum distance between each Global Props of this id.";

    public int ID;

    [Space]
    [Tooltip(MinimumDistanceTooltip)]
    public float MinimumDistance;

    public DetailedGlobalPropSettings(int id, float minimumDistance) {
      ID = id;
      MinimumDistance = minimumDistance;
    }
  }
}
