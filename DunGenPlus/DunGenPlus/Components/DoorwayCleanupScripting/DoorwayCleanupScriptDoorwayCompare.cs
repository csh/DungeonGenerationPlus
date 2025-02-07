using DunGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Components.DoorwayCleanupScripting {
  [Obsolete("Please use DoorwayScriptingParent")]
  public abstract class DoorwayCleanupScriptDoorwayCompare : DoorwayCleanupScript {

    public enum Operation { 
      [InspectorName("Equal (target == value)")]
      Equal, 
      [InspectorName("NotEqual (target != value)")]
      NotEqual, 
      [InspectorName("LessThan (target < value)")]
      LessThan, 
      [InspectorName("GreaterThan (target > value)")]
      GreaterThan, 
      [InspectorName("LessThanEq (target <= value)")]
      LessThanEq, 
      [InspectorName("GreaterThanEw (target >= value)")]
      GreaterThanEq, 
      [InspectorName("Between (value < target < valueB)")]
      Between,
      [InspectorName("BetweenEq (value <= target <= valueB)")]
      BetweenEq
    }
    
    public struct Arguments{
      public int parameterA;
      public int parameterB;

      public Arguments(int parameterA, int parameterB){
        this.parameterA = parameterA;
        this.parameterB = parameterB;
      }

      public static explicit operator Arguments((int a, int b) pair) => new Arguments(pair.a, pair.b);
    }

    public Func<Doorway, Arguments, bool> GetOperation(Operation operation){
      switch(operation){
        case Operation.Equal:
          return EqualOperation;
        case Operation.NotEqual:
          return NotEqualOperation;
        case Operation.LessThan:
          return LessThanOperation;
        case Operation.GreaterThan:
          return GreaterThanOperation;
        case Operation.LessThanEq:
          return LessThanEqualOperation;
        case Operation.GreaterThanEq:
          return GreaterThanEqualOperation;
        case Operation.Between:
          return BetweenOperation;
        case Operation.BetweenEq:
          return BetweenEqualOperation;
      }
      return null;
    }

    public bool EqualOperation(Doorway other, Arguments arguments){
      return other.DoorPrefabPriority == arguments.parameterA;
    }

    public bool NotEqualOperation(Doorway other, Arguments arguments){
      return other.DoorPrefabPriority != arguments.parameterA;
    }

    public bool LessThanOperation(Doorway other, Arguments arguments){
      return other.DoorPrefabPriority < arguments.parameterA;
    }

    public bool GreaterThanOperation(Doorway other, Arguments arguments){
      return other.DoorPrefabPriority > arguments.parameterA;
    }

    public bool LessThanEqualOperation(Doorway other, Arguments arguments){
      return other.DoorPrefabPriority <= arguments.parameterA;
    }

    public bool GreaterThanEqualOperation(Doorway other, Arguments arguments){
      return other.DoorPrefabPriority >= arguments.parameterA;
    }

    public bool BetweenOperation(Doorway other, Arguments arguments){
      return arguments.parameterA < other.DoorPrefabPriority && other.DoorPrefabPriority < arguments.parameterB;
    }

    public bool BetweenEqualOperation(Doorway other, Arguments arguments){
      return arguments.parameterA <= other.DoorPrefabPriority && other.DoorPrefabPriority <= arguments.parameterB;
    }

  }
}
