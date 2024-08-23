using DunGen;
using DunGen.Graph;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.DevTools.Panels {
  internal abstract class BasePanel : MonoBehaviour {

    public DevDebugManager manager => DevDebugManager.Instance;
    public RuntimeDungeon dungeon => manager.dungeon;
    public ExtendedDungeonFlow selectedExtendedDungeonFlow => manager.selectedDungeonFlow;
    public DungeonFlow selectedDungeonFlow => selectedExtendedDungeonFlow.DungeonFlow;
    
    [Header("Renders")]  
    public GameObject mainGameObject;
    public PanelTab tab;

    public virtual void AwakeCall() {
      
    }

    public virtual void SetPanelVisibility(bool visible) {
      mainGameObject.SetActive(visible);
      tab.active = visible;
    }

    protected int ParseTextInt(string text, int defaultValue = 0) {
      if (int.TryParse(text, out var result)){
        return result;
      } else {
        Plugin.logger.LogWarning($"Couldn't parse {text} into an int");
        return defaultValue;
      }
    }

    protected float ParseTextFloat(string text, float defaultValue = 0f) {
      if (float.TryParse(text, out var result)){
        return result;
      } else {
        Plugin.logger.LogWarning($"Couldn't parse {text} into a float");
        return defaultValue;
      }
    }


  }
}
