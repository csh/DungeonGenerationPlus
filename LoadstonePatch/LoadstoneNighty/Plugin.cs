using BepInEx.Bootstrap;
using BepInEx.Logging;
using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadstoneNighty {
  [BepInPlugin(modGUID, modName, modVersion)]
  
  [BepInDependency("dev.ladyalice.dungenplus", "1.2.1")]
  [BepInDependency(targetModGUID, BepInDependency.DependencyFlags.SoftDependency)]

  public class Plugin : BaseUnityPlugin {

    public const string modGUID = "dev.ladyalice.dungenplus.loadstonepatch";
    private const string modName = "Dungeon Generation Plus Loadstone Patch";
    private const string modVersion = "1.0.0";

    public const string targetModGUID = "com.adibtw.loadstone.Nightly";
    public const string targetModVersion = "0.1.16";

    public readonly Harmony harmony = new Harmony(modGUID);
    public static Plugin Instance {get; private set;}
    public static ManualLogSource logger { get; internal set; }

    void Awake(){
      if (Instance == null) Instance = this;
     
      logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);

       var modLoaded = Chainloader.PluginInfos.ContainsKey(targetModGUID);
      if (!modLoaded) return;

      bool validVersion;
      var pluginInfo = Chainloader.PluginInfos[targetModGUID];
      var loadedVersion = pluginInfo.Metadata.Version;
      if (string.IsNullOrWhiteSpace(targetModVersion)){
        validVersion = true;
      } else {
        var requiredVersion = new Version(targetModVersion);
        validVersion = loadedVersion >= requiredVersion;
      }

      if (validVersion){
        logger.LogInfo($"Plugin {modName} has been added!");
        Patch.Activate();
      }
    }
  }
}
