using DunGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DunGenPlus.Components.DoorwayCleanupScripting {
  public abstract class DoorwayCleanupScriptDoorwayCompare : DoorwayCleanupScript {

    public enum Operation { Equal, NotEqual, LessThan, GreaterThan }
    
    public Func<Doorway, int, bool> GetOperation(Operation operation){
      switch(operation){
        case Operation.Equal:
          return EqualOperation;
        case Operation.NotEqual:
          return NotEqualOperation;
        case Operation.LessThan:
          return LessThanOperation;
        case Operation.GreaterThan:
          return GreaterThanOperation;
      }
      return null;
    }

    public bool EqualOperation(Doorway other, int doorwayPriority){
      return other.DoorPrefabPriority == doorwayPriority;
    }

    public bool NotEqualOperation(Doorway other, int doorwayPriority){
      return other.DoorPrefabPriority != doorwayPriority;
    }

    public bool LessThanOperation(Doorway other, int doorwayPriority){
      return other.DoorPrefabPriority < doorwayPriority;
    }

    public bool GreaterThanOperation(Doorway other, int doorwayPriority){
      return other.DoorPrefabPriority > doorwayPriority;
    }

  }
}
