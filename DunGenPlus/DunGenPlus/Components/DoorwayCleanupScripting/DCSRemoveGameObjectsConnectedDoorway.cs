using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Components.DoorwayCleanupScripting {
  public class DCSRemoveGameObjectsConnectedDoorway : DoorwayCleanupScriptDoorwayCompare {

    [Header("Removes target GameObjects\nif the neighboring doorway's priority matches the operation comparison")]
    
    [Header("Operation Comparison")]
    public int doorwayPriority;
    public Operation operation = Operation.Equal;

    [Header("Targets")]
    public List<GameObject> targets;

    public override void Cleanup(DoorwayCleanup parent) {
      var doorway = parent.doorway;
      if (doorway.connectedDoorway == null) return;
      var result = GetOperation(operation).Invoke(doorway.connectedDoorway, doorwayPriority);
      if (result) {
        foreach(var t in targets) t.SetActive(false);
      }
    }

  }
}
