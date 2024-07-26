using DunGen;
using DunGenPlus.Components.DoorwayCleanupScripting;
using DunGenPlus.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Components {
  public class DoorwayCleanup : MonoBehaviour, IDungeonCompleteReceiver {

    [Header("Doorway References")]
    [Tooltip("The doorway reference.")]
    public Doorway doorway;
    [Tooltip("The connectors scene objects of the doorway.\n\nHighly advise to empty the corresponding list in the doorway.")]
    public List<GameObject> connectors;
    [Tooltip("The blockers scene objects of the doorway.\n\nHighly advise to empty the corresponding list in the doorway.")]
    public List<GameObject> blockers;
    [Tooltip("The doorway gameobject target for the DoorwayCleanupScripts. Can be null.")]
    public GameObject doorwayGameObject;

    [Header("Overrides")]
    [Tooltip("Mainly for code purposes. Forces the connectors to be active.")]
    public bool overrideConnector;
    [Tooltip("Mainly for code purposes. Forces the blockers to be active.")]
    public bool overrideBlocker;
    [Tooltip("Mainly for code purposes. Forces the doorway gameobject to be disabled.")]
    public bool overrideNoDoorway;

    public void OnDungeonComplete(Dungeon dungeon) {
      SetBlockers(true);
      DoorwayManager.AddDoorwayCleanup(this);
    }

    public void Cleanup(){
      // start up like in original
      SwitchConnectorBlocker(doorway.ConnectedDoorway != null);

      var cleanupList = GetComponentsInChildren<DoorwayCleanupScript>();
      foreach(var c in cleanupList) c.Cleanup(this);
        
      if (overrideNoDoorway) SwitchDoorwayGameObject(false);

      // clean up like in original
      foreach(var c in connectors){
        if (!c.activeSelf) UnityEngine.Object.DestroyImmediate(c, false);
      }

      foreach(var b in blockers){
        if (!b.activeSelf) UnityEngine.Object.DestroyImmediate(b, false);
      }
    }

    public void SetBlockers(bool state){
      foreach(var b in blockers) b.SetActive(state);
    }

    public void SwitchConnectorBlocker(bool isConnector){
      if (overrideConnector) isConnector = true;
      if (overrideBlocker) isConnector = false;

      foreach(var c in connectors) c.SetActive(isConnector);
      foreach(var b in blockers) b.SetActive(!isConnector);
    }

    public void SwitchDoorwayGameObject(bool isActive){
      doorwayGameObject?.SetActive(isActive);
    }
  }
}
