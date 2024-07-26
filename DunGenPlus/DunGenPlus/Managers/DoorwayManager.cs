using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DunGen.Adapters;
using DunGenPlus.Components;
using DunGenPlus.Generation;
using DunGenPlus.Utils;

namespace DunGenPlus.Managers {
  public static class DoorwayManager {

    public static ActionList onMainEntranceTeleportSpawnedEvent = new ActionList("onMainEntranceTeleportSpawned");
    public static List<DoorwayCleanup> doorwayCleanupList;

    public static void ResetList(){
      doorwayCleanupList = new List<DoorwayCleanup>();
    }

    public static void AddDoorwayCleanup(DoorwayCleanup cleanup){
      doorwayCleanupList.Add(cleanup);
    }

    public static void onMainEntranceTeleportSpawnedFunction(){
      if (DunGenPlusGenerator.Active) {
        foreach(var d in doorwayCleanupList){
          d.SetBlockers(false);
          d.Cleanup();
        }

        try{
          var dungeonGen = RoundManager.Instance.dungeonGenerator;
          var navmesh = dungeonGen.transform.parent.GetComponentInChildren<UnityNavMeshAdapter>();
          navmesh.Run(dungeonGen.Generator);
          Plugin.logger.LogInfo("Rebuild nav mesh");
        } catch (Exception e){
          Plugin.logger.LogError("Failed to rebuild nav mesh");
          Plugin.logger.LogError(e.ToString());
        }
        
      }
    }

  }
}
