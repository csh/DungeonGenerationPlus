using DunGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Collections {
  [System.Serializable]
  public class LocalGlobalPropSettings {

    internal const string GlobalPropLimitTooltip = "If true, when PostProcess reaches the local limit of Global Props for all main paths but does not reach the global limit, use the remaining props in this main path to reach the global limit.";
    internal const string MinimumDistanceBetweenPropsTooltip = "If true, Global Props of this id MUST have a minimum distance between each other.";

    public int ID;

    [Space]
    public IntRange Count;
    [Tooltip(GlobalPropLimitTooltip)]
    public bool UseToReachGlobalPropLimit;

    public LocalGlobalPropSettings(int id, IntRange count, bool useToReachGlobalPropLimit = false) {
      ID = id;
      Count = count;
      UseToReachGlobalPropLimit = useToReachGlobalPropLimit;
    }
  }
}
