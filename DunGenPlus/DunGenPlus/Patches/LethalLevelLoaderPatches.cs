using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DunGenPlus.DevTools;
using HarmonyLib;


namespace DunGenPlus.Patches {
  internal class LethalLevelLoaderPatches {

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LethalLevelLoader.Patches), "DungeonGeneratorGenerate_Prefix")]
    public static bool DungeonGeneratorGenerate_Prefix_Prefix(){
      return DevDebugManager.Instance == null;
    }

  }
}
