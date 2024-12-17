using DunGen.Graph;
using DunGen;
using DunGenPlus.Collections;
using DunGenPlus;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadstoneNighty {

  // patch 16 and below probably requires me to patch the function no matter what
  public class PatchVer16 {

    public static void Activate(){
      try {
        Plugin.Instance.harmony.PatchAll(typeof(PatchVer16));
      } catch {
          
      }
      Plugin.logger.LogInfo($"FromProxyEnd function has been patched!");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Loadstone.Patches.FromProxyPatches), "FromProxyEnd")]
    public static void FromProxyEndPatch(Dictionary<TileProxy, Tile> dictionary){
      DunGenPlus.API.AddTileToMainPathDictionary(dictionary);
    }

  }
}
