using DunGen;
using DunGenPlus.Managers;
using Soukoku.ExpressionParser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Components.Scripting {

  public enum OverrideGameObjectState {
    None,
    Active,
    Disabled
  }

  [System.Serializable]
  public class NamedGameObjectReference {
    public string name;
    public List<GameObject> gameObjects;
    public OverrideGameObjectState overrideState;

    public NamedGameObjectReference(string name, List<GameObject> gameObjects){
      this.name = name;
      this.gameObjects = gameObjects;
    }

    public void SetState(bool state){
      foreach(var g in gameObjects){
        g?.SetActive(state);
      }
    }

    public void FixOverrideState(){
      if (overrideState == OverrideGameObjectState.None) return;
      SetState(overrideState == OverrideGameObjectState.Active);
    }

    public void DestroyInactiveGameObjects(){
      foreach(var g in gameObjects){
        if (g && !g.activeSelf) UnityEngine.Object.DestroyImmediate(g, false);
      }
    }
  }

  public enum DunGenScriptingHook {
    SetLevelObjectVariables,
    OnMainEntranceTeleportSpawned
  }


  public interface IDunGenScriptingParent {

    DunGenScriptingHook GetScriptingHook { get; }

    void Call();

    List<NamedGameObjectReference> GetNamedReferences { get; }
    
    void AddNamedReference(string name, List<GameObject> gameObjects);

    void SetNamedGameObjectState(string name, bool state);
    void SetNamedGameObjectOverrideState(string name, OverrideGameObjectState state);

    EvaluationContext CreateContext();

  }

  public abstract class DunGenPlusScriptingParent<T> : MonoBehaviour, IDunGenScriptingParent, IDungeonCompleteReceiver where T: Component {

    public static bool InDebugMode => DunGenPlusScript.InDebugMode;

    [Header("REQUIRED")]
    [Tooltip("The target reference.")]
    public T targetReference;
    public DunGenScriptingHook callHook = DunGenScriptingHook.OnMainEntranceTeleportSpawned;

    [Header("Named References")]
    [Tooltip("Provide a variable name for a list of gameObjects. Used in DunGenScripting.")]
    public List<NamedGameObjectReference> namedReferences = new List<NamedGameObjectReference>();
    public Dictionary<string, NamedGameObjectReference> namedDictionary = new Dictionary<string, NamedGameObjectReference>();

    public DunGenScriptingHook GetScriptingHook => callHook;
    public List<NamedGameObjectReference> GetNamedReferences => namedReferences;

    public void OnDungeonComplete(Dungeon dungeon) {
      //SetBlockers(true);
      //Debug.Log("ONDUNGEONCOMPLETE");
      DoorwayManager.AddDunGenScriptHook(this);
    }

    public virtual void Awake(){
      foreach(var r in namedReferences){
        namedDictionary.Add(r.name, r);
      }
    } 

    public virtual void Call() {
      // call scripts
      var scripts = GetComponentsInChildren<DunGenPlusScript>();
      foreach(var c in scripts) c.Call(this);
        
      // apply any overrides
      foreach(var n in namedReferences) n.FixOverrideState();

      // clean up like in original
      foreach(var n in namedReferences) DestroyInactiveGameObjects(n.gameObjects);
    }

    public void AddNamedReference(string name, List<GameObject> gameObjects) {
      var item = new NamedGameObjectReference(name, gameObjects);
      namedReferences.Add(item);
      namedDictionary.Add(name, item);
    }

    public void SetNamedGameObjectState(string name, bool state){
      if (namedDictionary.TryGetValue(name, out var obj)){
        obj.SetState(state);
      } else {
        Plugin.logger.LogError($"Named reference: {name} does not exist");
      }
    }

    public void SetNamedGameObjectOverrideState(string name, OverrideGameObjectState state){
      if (namedDictionary.TryGetValue(name, out var obj)){
        obj.overrideState = state;
      }
    }

    public void DestroyInactiveGameObjects(IEnumerable<GameObject> gameObjects){
      foreach(var g in gameObjects) {
        if (g && !g.activeSelf) {
          UnityEngine.Object.DestroyImmediate(g, false);
        }
      }
    }

    protected bool CheckIfNotNull(object target, string name){
      if (target == null) {
        Utils.Utility.PrintLog($"{name} was null", BepInEx.Logging.LogLevel.Error);
        return false;
      }
      return true;
    }

    public abstract EvaluationContext CreateContext();

  }
}
