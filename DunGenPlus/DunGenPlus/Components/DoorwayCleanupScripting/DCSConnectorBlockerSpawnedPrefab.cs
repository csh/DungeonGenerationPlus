using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Components.DoorwayCleanupScripting {
  public class DCSConnectorBlockerSpawnedPrefab : DoorwayCleanupScript {

    public enum Action { SwitchToConnector, SwitchToBlocker };

    [Header("Calls switch action\nif Doorway instantiates a Connector/Blocker prefab with the target's name")]
    [Header("Switch Action")]
    public Action switchAction;

    [Header("Target")]
    public GameObject target;

    public override void Cleanup(DoorwayCleanup parent) {
      var result = false;
      foreach(Transform t in parent.doorway.transform){
        if (t.gameObject.activeSelf && t.name.Contains(target.name)) {
          result = true;
          break;
        }
      }

      if (result) {
        parent.SwitchConnectorBlocker(switchAction == Action.SwitchToConnector);
      }
    }

  }
}
