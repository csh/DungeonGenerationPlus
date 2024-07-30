using DunGenPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using UnityEngine;
using LethalLevelLoader;

namespace DunGenPlus {
  internal class Assets {
     
    public static void LoadAssets(){
      foreach (string text in Directory.GetFiles(Paths.PluginPath, "*.lethalbundle", SearchOption.AllDirectories)) {
				FileInfo fileInfo = new FileInfo(text);
        LethalLevelLoader.AssetBundleLoader.AddOnLethalBundleLoadedListener(AutoAddLethalBundle, fileInfo.Name);
			}
    }

    static void AutoAddLethalBundle(AssetBundle assetBundle){
      var extenders = assetBundle.LoadAllAssets<DunGenExtender>();
      var content = assetBundle.LoadAllAssets<ExtendedContent>();

      if (content.Length == 0) {
        Plugin.logger.LogWarning($".lethalbundle does not contain any ExtendedContent. Unless you are manually creating and adding your ExtendedDungeonFlow with code, the DunGenExtender will probably not work.");
      }

      foreach(var e in extenders){
        API.AddDunGenExtender(e);
      }
    }

  }
}
