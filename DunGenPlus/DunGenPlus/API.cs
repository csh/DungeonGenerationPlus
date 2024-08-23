using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using DunGen;
using DunGen.Graph;
using DunGenPlus.Generation;
using LethalLevelLoader;
using UnityEngine;

namespace DunGenPlus
{
  public class API {

    /// <summary>
    /// Registers the <paramref name="dungeonFlow"/> to recieve the alternate dungeon generation changes defined by <paramref name="dunGenExtender"/>.
    /// </summary>
    /// <param name="dungeonFlow"></param>
    /// <param name="dunGenExtender"></param>
    /// 
    /// <returns>
    /// <see langword="true"/> if <paramref name="dunGenExtender"/> was successfully added. 
    /// <see langword="false"/> if <paramref name="dungeonFlow"/> was null or already has a registered <see cref="DunGenExtender"/>.
    /// </returns>
    public static bool AddDunGenExtender(DungeonFlow dungeonFlow, DunGenExtender dunGenExtender) {
      if (dungeonFlow == null) {
        Plugin.logger.LogError("dungeonFlow was null");
        return false;
      }

      if (ContainsDungeonFlow(dungeonFlow)) {
        Plugin.logger.LogWarning($"Already contains DunGenExtender asset for {dungeonFlow.name}");
        return false;
      }

      Plugin.DunGenExtenders.Add(dungeonFlow, dunGenExtender);
      Plugin.logger.LogInfo($"Added DunGenExtender asset for {dungeonFlow.name}");

      return true;
    }

    /// <summary>
    /// Registers the <see cref="DunGenExtender.DungeonFlow"/> to recieve the alternate dungeon generation changes defined by <paramref name="dunGenExtender"/>.
    /// </summary>
    /// <param name="dunGenExtender"></param>
    /// 
    /// <returns>
    /// <see langword="true"/> if <paramref name="dunGenExtender"/> was successfully added. 
    /// <see langword="false"/> if <see cref="DunGenExtender.DungeonFlow"/> was null or already has a registered <see cref="DunGenExtender"/>.
    /// </returns>
    public static bool AddDunGenExtender(DunGenExtender dunGenExtender) {
      if (dunGenExtender == null) {
        Plugin.logger.LogError("dunGenExtender was null");
        return false;
      }

      return AddDunGenExtender(dunGenExtender.DungeonFlow, dunGenExtender);
    }

    /// <summary>
    /// Checks if <paramref name="dungeonFlow"/> has a registered <see cref="DunGenExtender"/>. 
    /// </summary>
    /// <param name="dungeonFlow"></param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="dungeonFlow"/> has a registered <see cref="DunGenExtender"/>. 
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool ContainsDungeonFlow(DungeonFlow dungeonFlow) {
      return Plugin.DunGenExtenders.ContainsKey(dungeonFlow);
    }

    /// <summary>
    /// Checks if <paramref name="extendedDungeonFlow"/> has a registered <see cref="DunGenExtender"/>. 
    /// </summary>
    /// <param name="extendedDungeonFlow"></param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="extendedDungeonFlow"/> has a registered <see cref="DunGenExtender"/>. 
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool ContainsDungeonFlow(ExtendedDungeonFlow extendedDungeonFlow) {
      if (extendedDungeonFlow == null) return false;
      return ContainsDungeonFlow(extendedDungeonFlow.DungeonFlow);
    }

    /// <summary>
    /// Returns corresponding <see cref="DunGenExtender"/> for <paramref name="dungeonFlow"/>.
    /// </summary>
    /// <param name="dungeonFlow"></param>
    /// <returns></returns>
    public static DunGenExtender GetDunGenExtender(DungeonFlow dungeonFlow) {
      if (Plugin.DunGenExtenders.TryGetValue(dungeonFlow, out var value)) {
        return value;
      }
      return null;
    }

    /// <summary>
    /// Creates and returns an empty <see cref="DunGenExtender"/>.
    /// </summary>
    /// <param name="dungeonFlow"></param>
    /// <returns>An empty <see cref="DunGenExtender"/>.</returns>
    public static DunGenExtender CreateDunGenExtender(DungeonFlow dungeonFlow){
      var extender = ScriptableObject.CreateInstance<DunGenExtender>();
      extender.DungeonFlow = dungeonFlow;
      
      return extender;
    }

  }
}
