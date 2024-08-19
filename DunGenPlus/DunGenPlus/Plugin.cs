using BepInEx;
using BepInEx.Logging;
using DunGen;
using DunGen.Graph;
using DunGenPlus.Generation;
using DunGenPlus.Managers;
using DunGenPlus.Patches;
using HarmonyLib;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace DunGenPlus {

  [BepInPlugin(modGUID, modName, modVersion)]
  [BepInProcess("Lethal Company.exe")]
  public class Plugin : BaseUnityPlugin {

    internal const string modGUID = "dev.ladyalice.dungenplus";
    private const string modName = "Dungeon Generation Plus";
    private const string modVersion = "1.0.6";

    internal readonly Harmony Harmony = new Harmony(modGUID);

    internal static Plugin Instance {get; private set;}

    internal static ManualLogSource logger { get; private set; }

    internal static Dictionary<DungeonFlow, DunGenExtender> DunGenExtenders = new Dictionary<DungeonFlow, DunGenExtender>();

    void Awake() {
      if (Instance == null)  Instance = this;

      logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);
      logger.LogInfo($"Plugin {modName} has been added!");

      Harmony.PatchAll(typeof(DungeonGeneratorPatch));
      Harmony.PatchAll(typeof(DoorwayConnectionPatch));
      Harmony.PatchAll(typeof(RoundManagerPatch));

      //Harmony.PatchAll(typeof(StartOfRoundPatch));

      Assets.LoadAssets();
      DungeonManager.GlobalDungeonEvents.onBeforeDungeonGenerate.AddListener(OnDunGenExtenderLoad);
      DoorwayManager.onMainEntranceTeleportSpawnedEvent.AddEvent("DoorwayCleanup", DoorwayManager.onMainEntranceTeleportSpawnedFunction);
    }

    void OnDunGenExtenderLoad(RoundManager roundManager) {
      DunGenPlusGenerator.Deactivate();

      var generator = roundManager.dungeonGenerator.Generator;
      var flow = generator.DungeonFlow;
      if (DunGenExtenders.TryGetValue(flow, out var value)) {
        Plugin.logger.LogInfo($"Loading DunGenExtender for {flow.name}");
        DunGenPlusGenerator.Activate(generator, value);
        return;
      }

      Plugin.logger.LogInfo($"Did not load a DunGenExtenderer");
      DunGenPlusGenerator.Deactivate();
    }

  }
}
