using DunGen;
using DunGen.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DunGenPlus.Collections;

namespace DunGenPlus {
  
  [CreateAssetMenu(fileName = "DunGen Extender", menuName = "DunGenExtender/DunGen Extender", order = 1)]
  public class DunGenExtender : ScriptableObject {

    [Tooltip("DunGenExtender will only influence this DungeonFlow")]
    public DungeonFlow DungeonFlow;
    public DunGenExtenderProperties Properties = new DunGenExtenderProperties();
    public DunGenExtenderEvents Events = new DunGenExtenderEvents();

    [Header("DEV ONLY: DON'T TOUCH")]
    [Attributes.ReadOnly]
    public string Version = "0";
    internal bool Active = true;

  }
}
