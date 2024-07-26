using DunGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Components {
  public class MainRoomDoorwayGroups : MonoBehaviour {

    [System.Serializable]
    public class DoorwayList {
      [Tooltip("For organizing purposes. Has no effect.")]
      public string name;
      [Tooltip("The group of doorways.")]
      public List<Doorway> doorways;

      public bool Contains(Doorway target) {
        return doorways.Contains(target);
      }
    }

    [Tooltip("When an additional main path is being generated, it will get the doorway used for the previous main path, find it's corresponding group below, and prevents the dungeon generation from using that group's doorways until the main paths are all generated.\n\nIf you want this feature, this must be attached to the tile that will act as the MainRoomTilePrefab.\n\nThis is designed for the scenario where you would like the main paths to be generated more evenly throughout the MainRoomTilePrefab.")]
    public List<DoorwayList> doorwayLists;
    public List<Doorway> doorwayListFirst => doorwayLists.Count > 0 ? doorwayLists[0].doorways : null;

    public List<Doorway> GrabDoorwayGroup(Doorway target){
      foreach(var a in doorwayLists){
        if (a.Contains(target)) return a.doorways;
      }
      return null;
    }

  }
}
