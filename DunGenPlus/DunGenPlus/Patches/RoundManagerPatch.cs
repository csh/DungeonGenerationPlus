using DunGen;
using DunGenPlus.Components.Scrap;
using DunGenPlus.Generation;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;

namespace DunGenPlus.Patches {
  public class RoundManagerPatch {

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RoundManager), "waitForScrapToSpawnToSync")]
    public static void waitForScrapToSpawnToSyncPatch (ref RoundManager __instance, ref NetworkObjectReference[] spawnedScrap, ref int[] scrapValues) {
      if (DunGenPlusGenerator.Active) {
        var spawnedScrapList = spawnedScrap.ToList();
        var scrapValuesList = scrapValues.ToList();

        var sources = UnityEngine.Object.FindObjectsOfType<RandomGuaranteedScrapSpawn>();
        RandomGuaranteedScrapSpawn.ResetCache();
        foreach(var s in sources) {
          var result = s.CreateItem(__instance, __instance.currentLevel.spawnableScrap);
          if (result.itemReference != null) {
            Plugin.logger.LogInfo($"Created guaranteed item {result.itemReference.gameObject.name} w/ value {result.scrapValue}");
            spawnedScrapList.Add(result.itemReference);
            scrapValuesList.Add(result.scrapValue);
          }
        }

        spawnedScrap = spawnedScrapList.ToArray();
        scrapValues = scrapValuesList.ToArray();
      }
    }

  }
}
