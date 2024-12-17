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

namespace DunGenPlus.Patches {
  internal class DungeonGeneratorPatch {

    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(DungeonGenerator), "Generate")]
    [HarmonyPrefix]
    public static void GeneratePatch(ref DungeonGenerator __instance){
      DunGenPlusGenerator.Deactivate();

      var flow = __instance.DungeonFlow;
      var extender = API.GetDunGenExtender(flow);
      if (extender && extender.Active) {
        Plugin.logger.LogInfo($"Loading DunGenExtender for {flow.name}");
        DunGenPlusGenerator.Activate(__instance, extender);
        return;
      }

      Plugin.logger.LogInfo($"Did not load a DunGenExtenderer");
      DunGenPlusGenerator.Deactivate();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DungeonGenerator), "InnerGenerate")]
    public static void InnerGeneratePatch(ref DungeonGenerator __instance, bool isRetry, ref IEnumerator __result){
      //Plugin.logger.LogWarning($"InnerGenerate: {DunGenPlusGenerator.Active}, {DunGenPlusGenerator.ActiveAlternative}, {__instance.Status}");
      if (API.IsDevDebugModeActive() && !isRetry) {
        DevDebugManager.Instance.RecordNewSeed(__instance.ChosenSeed);
      }

      if (DunGenPlusGenerator.Active && DunGenPlusGenerator.ActiveAlternative) {
        DunGenPlusGenerator.SetCurrentMainPathExtender(0);
        MainRoomDoorwayGroups.ModifyGroupBasedOnBehaviourSimpleOnce = false;
      }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DungeonGenerator), "GenerateMainPath")]
    public static void GenerateMainPathPatch(ref DungeonGenerator __instance, ref IEnumerator __result){
      //Plugin.logger.LogWarning($"GenerateMainPath: {DunGenPlusGenerator.Active}, {DunGenPlusGenerator.ActiveAlternative}, {__instance.Status}");
      if (DunGenPlusGenerator.Active && DunGenPlusGenerator.ActiveAlternative) {
        DunGenPlusGenerator.RandomizeLineArchetypes(__instance, true);
      }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DungeonGenerator), "GenerateBranchPaths")]
    public static void GenerateBranchPathsPatch(ref DungeonGenerator __instance, ref IEnumerator __result){
      //Plugin.logger.LogWarning($"GenerateBranchPaths: {DunGenPlusGenerator.Active}, {DunGenPlusGenerator.ActiveAlternative}, {__instance.Status}");
      if (DunGenPlusGenerator.Active && DunGenPlusGenerator.ActiveAlternative) {
        __result = DunGenPlusGenerator.GenerateAlternativeMainPaths(__instance); 
      }

    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DungeonGenerator), "GenerateMainPath", MethodType.Enumerator)]
    public static IEnumerable<CodeInstruction> GenerateMainPathPatch(IEnumerable<CodeInstruction> instructions){
      
      var addArchFunction = typeof(List<DungeonArchetype>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);

      var archSequence = new InstructionSequenceStandard("archetype node");
      archSequence.AddOperandTypeCheck(OpCodes.Ldfld, typeof(List<DungeonArchetype>));
      archSequence.AddBasic(OpCodes.Ldnull);
      archSequence.AddBasic(OpCodes.Callvirt, addArchFunction);

      var attachToSequence = new InstructionSequenceStandard("attach to");
      attachToSequence.AddBasicLocal(OpCodes.Stloc_S, 13);

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

        if (attachToSequence.VerifyStage(instruction)){
          yield return instruction;

          var modifyMethod = typeof(MainRoomDoorwayGroups).GetMethod("ModifyGroupBasedOnBehaviourSimple", BindingFlags.Public | BindingFlags.Static);

          yield return new CodeInstruction(OpCodes.Ldloc_S, 13);
          yield return new CodeInstruction(OpCodes.Call, modifyMethod);

          continue;
        }

        yield return instruction;
      }

      archSequence.ReportComplete();
      attachToSequence.ReportComplete();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DungeonGenerator), "GenerateMainPath", MethodType.Enumerator)]
    public static IEnumerable<CodeInstruction> GenerateMainPathGetLineAtDepthPatch(IEnumerable<CodeInstruction> instructions){

      var getLineFunction = typeof(DungeonFlow).GetMethod("GetLineAtDepth", BindingFlags.Instance | BindingFlags.Public);
      var nodesField = typeof(DungeonFlow).GetField("Nodes", BindingFlags.Instance | BindingFlags.Public);

      var lineSequence = new InstructionSequenceStandard("GetLineAtDepth");
      lineSequence.AddBasic(OpCodes.Callvirt, getLineFunction);

      var nodesSequence = new InstructionSequenceStandard("Nodes", false);
      nodesSequence.AddBasic(OpCodes.Ldfld, nodesField);

      foreach(var instruction in instructions){
        if (lineSequence.VerifyStage(instruction)) {
          var specialFunction = typeof(DunGenPlusGenerator).GetMethod("GetLineAtDepth", BindingFlags.Static | BindingFlags.Public);

          yield return new CodeInstruction(OpCodes.Call, specialFunction);

          continue;
        }

        if (nodesSequence.VerifyStage(instruction)) {
          var specialFunction = typeof(DunGenPlusGenerator).GetMethod("GetNodes", BindingFlags.Static | BindingFlags.Public); 

          yield return new CodeInstruction(OpCodes.Call, specialFunction);

          continue;
        }


        yield return instruction;
      }

      lineSequence.ReportComplete();
      nodesSequence.ReportComplete();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BranchCountHelper), "ComputeBranchCounts")]
    public static IEnumerable<CodeInstruction> ComputeBranchCountsPatch(IEnumerable<CodeInstruction> instructions){

      var branchModeField = typeof(DungeonFlow).GetField("BranchMode", BindingFlags.Instance | BindingFlags.Public);

      var branchSequence = new InstructionSequenceStandard("BranchMode", false);
      branchSequence.AddBasic(OpCodes.Ldfld, branchModeField);

      foreach(var instruction in instructions){
        if (branchSequence.VerifyStage(instruction)) {
          var specialFunction = typeof(DunGenPlusGenerator).GetMethod("GetBranchMode", BindingFlags.Static | BindingFlags.Public);

          yield return new CodeInstruction(OpCodes.Call, specialFunction);

          continue;
        }

        yield return instruction;
      }

      branchSequence.ReportComplete();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BranchCountHelper), "ComputeBranchCountsGlobal")]
    public static IEnumerable<CodeInstruction> ComputeBranchCountsGlobalPatch(IEnumerable<CodeInstruction> instructions){

      var branchCountField = typeof(DungeonFlow).GetField("BranchCount", BindingFlags.Instance | BindingFlags.Public);

      var branchSequence = new InstructionSequenceStandard("BranchCount");
      branchSequence.AddBasic(OpCodes.Ldfld, branchCountField);

      foreach(var instruction in instructions){
        if (branchSequence.VerifyStage(instruction)) {
          var specialFunction = typeof(DunGenPlusGenerator).GetMethod("GetBranchCount", BindingFlags.Static | BindingFlags.Public);

          yield return new CodeInstruction(OpCodes.Call, specialFunction);

          continue;
        }

        yield return instruction;
      }

      branchSequence.ReportComplete();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DungeonGenerator), "InnerGenerate", MethodType.Enumerator)]
    public static IEnumerable<CodeInstruction> InnerGenerateLengthPatch(IEnumerable<CodeInstruction> instructions){

      var lengthField = typeof(DungeonFlow).GetField("Length", BindingFlags.Instance | BindingFlags.Public);
      var getIsEditor = typeof(Application).GetMethod("get_isEditor", BindingFlags.Static | BindingFlags.Public);

      var lengthSequence = new InstructionSequenceStandard("Length");
      lengthSequence.AddBasic(OpCodes.Ldfld, lengthField);

      var editorCheck = new InstructionSequenceStandard("Editor");
      editorCheck.AddBasic(OpCodes.Call, getIsEditor);

      foreach(var instruction in instructions){
        if (lengthSequence.VerifyStage(instruction)) {
          var specialFunction = typeof(DunGenPlusGenerator).GetMethod("GetLength", BindingFlags.Static | BindingFlags.Public);

          yield return new CodeInstruction(OpCodes.Call, specialFunction);

          continue;
        }

        if (editorCheck.VerifyStage(instruction)){
          var specialFunction = typeof(DunGenPlusGenerator).GetMethod("AllowRetryStop", BindingFlags.Static | BindingFlags.Public);
          
          yield return instruction;
          yield return new CodeInstruction(OpCodes.Call, specialFunction);

          continue;
        }

        yield return instruction;
      }

      lengthSequence.ReportComplete();
      editorCheck.ReportComplete();
    }

    /*
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DungeonGenerator), "AddTile")]
    public static IEnumerable<CodeInstruction> AddTilePatch(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {

      var getCountFunction = typeof(Queue<DoorwayPair>).GetMethod("get_Count", BindingFlags.Instance | BindingFlags.Public);

      var whileLoopSequence = new InstructionSequenceHold("while loop");
      whileLoopSequence.AddBasic(OpCodes.Br);
      whileLoopSequence.AddBasicLocal(OpCodes.Ldloc_S, 8);
      whileLoopSequence.AddAny();
      whileLoopSequence.AddBasic(OpCodes.Ldc_I4_0);
      whileLoopSequence.AddBasic(OpCodes.Bgt);

      foreach(var instruction in instructions){
        var yieldInstruction = true;
        var result = whileLoopSequence.VerifyStage(instruction);

        switch(result) {
          case InstructionSequenceHold.HoldResult.None:
            break;
          case InstructionSequenceHold.HoldResult.Hold:
            yieldInstruction = false;
            break;
          case InstructionSequenceHold.HoldResult.Release:
            foreach(var i in whileLoopSequence.Instructions){
              yield return i;
            }
            whileLoopSequence.ClearInstructions();
            yieldInstruction = false;
            break;
          case InstructionSequenceHold.HoldResult.Finished:
            
            // my special function
            var specialFunction = typeof(DunGenPlusGenerator).GetMethod("ProcessDoorwayPairs");
            var resultLocal = generator.DeclareLocal(typeof((TilePlacementResult result, TileProxy tile)));

            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_S, 4);
            yield return new CodeInstruction(OpCodes.Ldloc_S, 8);
            yield return new CodeInstruction(OpCodes.Call, specialFunction);

            var item1Field = typeof((TilePlacementResult result, TileProxy tile)).GetField("Item1");
            var item2Field = typeof((TilePlacementResult result, TileProxy tile)).GetField("Item2");

            yield return new CodeInstruction(OpCodes.Stloc_S, resultLocal);
            
            yield return new CodeInstruction(OpCodes.Ldloc_S, resultLocal);
            yield return new CodeInstruction(OpCodes.Ldfld, item1Field);
            yield return new CodeInstruction(OpCodes.Stloc_S, 9);

            yield return new CodeInstruction(OpCodes.Ldloc_S, resultLocal);
            yield return new CodeInstruction(OpCodes.Ldfld, item2Field);
            yield return new CodeInstruction(OpCodes.Stloc_S, 10);

            whileLoopSequence.ClearInstructions();
            yieldInstruction = false;

            break;
        }

        if (yieldInstruction) yield return instruction;
      }

      whileLoopSequence.ReportComplete();

    }

    */

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RoundManager), "FinishGeneratingLevel")]
    public static void GenerateBranchPathsPatch(){
      if (DunGenPlusGenerator.Active) {
        Plugin.logger.LogDebug("Alt. InnerGenerate() function complete");
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

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DungeonGenerator), "AddTile")]
    public static IEnumerable<CodeInstruction> AddTileDebugPatch(IEnumerable<CodeInstruction> instructions){

      var addTileSequence = new InstructionSequenceStandard("Add Tile Placement");
      addTileSequence.AddBasic(OpCodes.Callvirt);
      addTileSequence.AddBasic(OpCodes.Ldc_I4_0);
      addTileSequence.AddBasic(OpCodes.Bgt);
      addTileSequence.AddBasicLocal(OpCodes.Ldloc_S, 9);

      foreach(var instruction in instructions){
        if (addTileSequence.VerifyStage(instruction)) {
          var specialFunction = typeof(DunGenPlusGenerator).GetMethod("RecordLastTilePlacementResult", BindingFlags.Static | BindingFlags.Public);

          yield return new CodeInstruction(OpCodes.Ldarg_0);
          yield return new CodeInstruction(OpCodes.Ldloc_S, 9);
          yield return new CodeInstruction(OpCodes.Call, specialFunction);

          yield return instruction;

          continue;
        }
        yield return instruction;
      }

      addTileSequence.ReportComplete();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Dungeon), "FromProxy")]
    public static IEnumerable<CodeInstruction> FromProxyPatch(IEnumerable<CodeInstruction> instructions){

      var endSequence = new InstructionSequenceStandard("Forloop End");
      endSequence.AddBasicLocal(OpCodes.Ldloca_S, 1);
      endSequence.AddBasic(OpCodes.Constrained);
      endSequence.AddBasic(OpCodes.Callvirt);
      endSequence.AddBasic(OpCodes.Endfinally);

      foreach(var instruction in instructions){
        if (endSequence.VerifyStage(instruction)) {
          yield return instruction;

          var specialFunction = typeof(DunGenPlusGenerator).GetMethod("AddTileToMainPathDictionary", BindingFlags.Static | BindingFlags.Public);

          yield return new CodeInstruction(OpCodes.Ldloc_0);
          yield return new CodeInstruction(OpCodes.Call, specialFunction);

          continue;
        }
        yield return instruction;
      }

      endSequence.ReportComplete();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DungeonGenerator), "ProcessGlobalProps")]
    public static bool ProcessGlobalPropsPatch(ref DungeonGenerator __instance){
      if (DunGenPlusGenerator.Active && DunGenPlusGenerator.Properties.MainPathProperties.MainPathDetails.Any(d => d.LocalGroupProps.Count > 0)){
        Plugin.logger.LogDebug("Performing Local Global Props algorithm");
        DunGenPlusGenerator.ProcessGlobalPropsPerMainPath(__instance);
        return false;
      }
      return true;
    }
    

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DungeonGenerator), "GenerateMainPath", MethodType.Enumerator)]
    public static IEnumerable<CodeInstruction> GenerateMainPathDebugPatch(IEnumerable<CodeInstruction> instructions){

      var tileProxyNullSequence = new InstructionSequenceStandard("TileProxyNull");
      tileProxyNullSequence.AddBasic(OpCodes.Br);
      tileProxyNullSequence.AddBasicLocal(OpCodes.Ldloc_S, 14);
      tileProxyNullSequence.AddBasic(OpCodes.Brtrue);

      foreach(var instruction in instructions){
        if (tileProxyNullSequence.VerifyStage(instruction)) {
          var specialFunction = typeof(DunGenPlusGenerator).GetMethod("PrintAddTileErrorQuick", BindingFlags.Static | BindingFlags.Public);
          var field = typeof(DungeonGenerator).Assembly.GetType("DunGen.DungeonGenerator+<GenerateMainPath>d__100").GetField("<j>5__8", BindingFlags.NonPublic | BindingFlags.Instance);

          yield return instruction;

          yield return new CodeInstruction(OpCodes.Ldloc_1);
          yield return new CodeInstruction(OpCodes.Ldarg_0);
          yield return new CodeInstruction(OpCodes.Ldfld, field);

          yield return new CodeInstruction(OpCodes.Call, specialFunction);

          continue;
        }

        yield return instruction;
      }

      tileProxyNullSequence.ReportComplete();
    }


    public static TileProxy lastAttachTo;
    public static IEnumerable<TileSet> lastUseableTileSets;
    public static float lastNormalizedDepth;
    public static DungeonArchetype lastArchetype;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DungeonGenerator), "AddTile")]
    public static void AddTileDebugPatch(TileProxy attachTo, IEnumerable<TileSet> useableTileSets, float normalizedDepth, DungeonArchetype archetype){
      lastAttachTo = attachTo;
      lastUseableTileSets = useableTileSets;
      lastNormalizedDepth = normalizedDepth;
      lastArchetype = archetype;
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
