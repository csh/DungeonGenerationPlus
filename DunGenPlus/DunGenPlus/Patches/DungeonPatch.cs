using DunGen;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using DunGenPlus.Utils;
using DunGenPlus.Generation;
using DunGenPlus.Managers;
using DunGenPlus.Collections;
using DunGenPlus.DevTools;
using DunGen.Graph;
using UnityEngine;
using DunGenPlus.Components;

namespace DunGenPlus.Patches
{
  [HarmonyPatch]
  internal class DungeonPatch {

    static MethodBase TargetMethod()
    {
      return AccessTools.EnumeratorMoveNext(
        AccessTools.Method(typeof(Dungeon), "FromProxy")
      );
    }
    
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original){
      var addMethod = AccessTools.Method(
        typeof(DunGenPlusGenerator),
        nameof(DunGenPlusGenerator.AddTileToMainPathDictionary)
      );

      var proxyDungeonField = AccessTools.Field(original.DeclaringType, "proxyDungeon");
      var dictField = AccessTools.Field(original.DeclaringType, "<proxyToTileMap>5__2");

      var proxyField = AccessTools.Field(typeof(DungeonProxy), "Connections");
      var getEnumerator = AccessTools.Method(typeof(List<ProxyDoorwayConnection>), "GetEnumerator");

      var endSequence = new InstructionSequenceStandard("Start Connections Loop");
      endSequence.AddBasic(OpCodes.Ldarg_0);
      endSequence.AddBasic(OpCodes.Ldfld, proxyDungeonField);
      endSequence.AddBasic(OpCodes.Ldfld, proxyField);
      endSequence.AddBasic(OpCodes.Callvirt, getEnumerator);

      foreach (var instruction in instructions)
      {
        if (endSequence.VerifyStage(instruction))
        {
          yield return new CodeInstruction(OpCodes.Ldarg_0);
          yield return new CodeInstruction(OpCodes.Ldfld, dictField);
          yield return new CodeInstruction(OpCodes.Call, addMethod);
        }

        yield return instruction;
      }

      endSequence.ReportComplete();
    }

  }
}
