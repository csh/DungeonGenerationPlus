using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DunGen;
using DunGen.Graph;

namespace DunGenPlus
{
  public class API {

    public static bool AddDunGenExtender(DungeonFlow dungeonFlow, DunGenExtender dunGenExtender) {
      if (dungeonFlow == null) {
        Plugin.logger.LogError("dungeonFlow was null");
        return false;
      }

      if (Plugin.DunGenExtenders.ContainsKey(dungeonFlow)) {
        Plugin.logger.LogWarning($"Already contains DunGenExtender asset for {dungeonFlow.name}");
        return false;
      }

      Plugin.DunGenExtenders.Add(dungeonFlow, dunGenExtender);
      Plugin.logger.LogInfo($"Added DunGenExtender asset for {dungeonFlow.name}");

      return true;
    }

    public static bool AddDunGenExtender(DunGenExtender dunGenExtender) {
      if (dunGenExtender == null) {
        Plugin.logger.LogError("dunGenExtender was null");
        return false;
      }

      return AddDunGenExtender(dunGenExtender.DungeonFlow, dunGenExtender);
    }

  }
}
