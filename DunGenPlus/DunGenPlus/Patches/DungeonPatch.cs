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
  internal class DungeonPatch {

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Dungeon), "FromProxy")]
    public static IEnumerable<CodeInstruction> FromProxyPatch(IEnumerable<CodeInstruction> instructions){
      var endSequence = new InstructionSequenceStandard("Forloop End");
      endSequence.AddBasicLocal(OpCodes.Ldloca_S, 1);
      endSequence.AddBasic(OpCodes.Constrained);
      endSequence.AddBasic(OpCodes.Callvirt);
      endSequence.AddBasic(OpCodes.Endfinally);

      // WE MUST INJECT BEFORE ENDFINALLY
      // DiFFoZ says cause try/catch block something
      // Idk that makes no sense
      // But if it works it works

      foreach(var instruction in instructions){
        if (endSequence.VerifyStage(instruction)) {
          var specialFunction = typeof(DunGenPlusGenerator).GetMethod("AddTileToMainPathDictionary", BindingFlags.Static | BindingFlags.Public);

          yield return new CodeInstruction(OpCodes.Ldloc_0);
          yield return new CodeInstruction(OpCodes.Call, specialFunction);
        }
        yield return instruction;
      }

      endSequence.ReportComplete();
    }

  }
}
