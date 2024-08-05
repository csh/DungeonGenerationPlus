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

namespace DunGenPlus.Patches {
  internal class DungeonGeneratorPatch {

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DungeonGenerator), "GenerateMainPath")]
    public static void GenerateMainPathPatch(ref DungeonGenerator __instance, ref IEnumerator __result){
      if (DunGenPlusGenerator.Active && DunGenPlusGenerator.ActiveAlternative) {
        DunGenPlusGenerator.RandomizeLineArchetypes(__instance, true);
      }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DungeonGenerator), "GenerateBranchPaths")]
    public static void GenerateBranchPathsPatch(ref DungeonGenerator __instance, ref IEnumerator __result){
      if (DunGenPlusGenerator.Active && DunGenPlusGenerator.ActiveAlternative) {
        __result = DunGenPlusGenerator.GenerateAlternativeMainPaths(__instance); 
      }

    }


    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DungeonGenerator), "GenerateMainPath", MethodType.Enumerator)]
    public static IEnumerable<CodeInstruction> GenerateMainPathPatch(IEnumerable<CodeInstruction> instructions){
      
      var addArchFunction = typeof(List<DungeonArchetype>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);

      var archSequence = new InstructionSequence("archetype node");
      archSequence.AddOperandTypeCheck(OpCodes.Ldfld, typeof(List<DungeonArchetype>));
      archSequence.AddBasic(OpCodes.Ldnull);
      archSequence.AddBasic(OpCodes.Callvirt, addArchFunction);

      foreach(var instruction in instructions){

        if (archSequence.VerifyStage(instruction)){

          var randomStreamMethod = typeof(DungeonGenerator).GetMethod("get_RandomStream", BindingFlags.Public | BindingFlags.Instance);
          var modifyMethod = typeof(DunGenPlusGenerator).GetMethod("ModifyMainBranchNodeArchetype", BindingFlags.Public | BindingFlags.Static);

          yield return new CodeInstruction(OpCodes.Ldloc_S, 8);
          yield return new CodeInstruction(OpCodes.Ldloc_1);
          yield return new CodeInstruction(OpCodes.Call, randomStreamMethod);
          yield return new CodeInstruction(OpCodes.Call, modifyMethod);
          yield return instruction;

          continue;
        }

        yield return instruction;
      }

      archSequence.ReportComplete();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RoundManager), "FinishGeneratingLevel")]
    public static void GenerateBranchPathsPatch(){
      if (DunGenPlusGenerator.Active) {
        Plugin.logger.LogInfo("Alt. InnerGenerate() function complete");
      }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RoundManager), "SetPowerOffAtStart")]
    public static void SetPowerOffAtStartPatch(){
      DoorwayManager.onMainEntranceTeleportSpawnedEvent.Call();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DungeonGenerator), "PostProcess")]
    public static void GenerateBranchPathsPatch(ref DungeonGenerator __instance){
      if (DunGenPlusGenerator.Active) {
        var value = __instance.RandomStream.Next(999);
        Components.Props.SpawnSyncedObjectCycle.UpdateCycle(value);
      }
    }

    /*
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DungeonGenerator), "GenerateMainPath", MethodType.Enumerator)]
    public static IEnumerable<CodeInstruction> GenerateMainPathPatch(IEnumerable<CodeInstruction> instructions){
      
      var addArchFunction = typeof(List<DungeonArchetype>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
      //var addNodeFunction = typeof(List<GraphNode>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);

      var archSequence = new InstructionSequence("archetype node");
      archSequence.AddOperandTypeCheck(OpCodes.Ldfld, typeof(List<DungeonArchetype>));
      archSequence.AddBasic(OpCodes.Ldnull);
      archSequence.AddBasic(OpCodes.Callvirt, addArchFunction);

      var nodeSequence = new InstructionSequence("graph node");
      nodeSequence.AddBasicLocal(OpCodes.Ldloc_S, 12);
      nodeSequence.AddBasicLocal(OpCodes.Stloc_S, 8);

      var limitSequence = new InstructionSequence("limit nodes");
      limitSequence.AddBasic(OpCodes.Ldnull);
      limitSequence.AddBasicLocal(OpCodes.Stloc_S, 13);
      limitSequence.AddBasic(OpCodes.Ldloc_1);
      limitSequence.AddBasicLocal(OpCodes.Ldloc_S, 13);

      foreach(var instruction in instructions){
      
        if (archSequence.VerifyStage(instruction)){

          var method = typeof(GeneratePath).GetMethod("ModifyMainBranchNodeArchetype", BindingFlags.Public | BindingFlags.Static);

          yield return new CodeInstruction(OpCodes.Ldloc_S, 8);
          yield return new CodeInstruction(OpCodes.Call, method);
          yield return instruction;

          continue;
        }
        

        if (nodeSequence.VerifyStage(instruction)){

          var method = typeof(GeneratePath).GetMethod("ModifyGraphNode", BindingFlags.Public | BindingFlags.Static);

          yield return new CodeInstruction(OpCodes.Call, method);
          yield return instruction;

          continue;
        }

        if (limitSequence.VerifyStage(instruction)){  

          var method = typeof(GeneratePath).GetMethod("LimitTilesToFirstFloor", BindingFlags.Public | BindingFlags.Static);
          var field = typeof(DungeonGenerator).Assembly.GetType("DunGen.DungeonGenerator+<GenerateMainPath>d__100").GetField("<j>5__8", BindingFlags.NonPublic | BindingFlags.Instance);

          yield return instruction;
          yield return new CodeInstruction(OpCodes.Ldarg_0);
          yield return new CodeInstruction(OpCodes.Ldfld, field);
          

          yield return new CodeInstruction(OpCodes.Call, method);
           
          continue;
        }

        yield return instruction;
      }

      archSequence.ReportComplete();
      nodeSequence.ReportComplete();
      limitSequence.ReportComplete();
    }
    */

  }
}
