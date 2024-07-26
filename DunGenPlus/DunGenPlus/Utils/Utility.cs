using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DunGenPlus.Utils {
  public class ActionList {
    public string name;
    public List<(string name, Action action)> actionList;

    public ActionList(string name){
      this.name = name;
      actionList = new List<(string, Action)>();
    }

    public void AddEvent(string name, Action act){
      actionList.Add((name, act));
    }

    public void Call(){
      foreach(var pair in actionList){
        try {
          pair.action.Invoke();
        } catch (Exception e) {
          Plugin.logger.LogError($"Error with event {name}/{pair.name}");
          Plugin.logger.LogError(e.ToString());
        }
      }
    }
  }

}
