using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using GameNetcodeStuff;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using UnityEngine;
using DunGenPlus;

namespace DunGenPlus.Utils {

  internal class InjectionDictionary {

    public string name;
    public List<CodeInstruction> instructions;
    public CodeInstruction[] injections;

    int counter;

    public InjectionDictionary(string name, MethodInfo methodInjection, params CodeInstruction[] instructions) {
      this.name = name;
      this.injections = new CodeInstruction[] { new CodeInstruction(OpCodes.Call, methodInjection) } ;
      this.instructions = instructions.ToList();
    }

    public InjectionDictionary(string name, CodeInstruction[] codeInjections, params CodeInstruction[] instructions) {
      this.name = name;
      this.injections = codeInjections;
      this.instructions = instructions.ToList();
    }

    public void ResetCounter(){
      counter = 0;
    }

    public void AddCounter() {
      counter++;
    }

    public void Report(string debugFunction, int? expectedCounter){
      if (counter == 0) {
        Plugin.logger.LogError($"{debugFunction} could not inject {name}. Probably scary");
      } else if (!expectedCounter.HasValue) {
        Plugin.logger.LogInfo($"{debugFunction} inject {name} {counter} time(s)");
      } else if (expectedCounter.Value != counter){
        Plugin.logger.LogWarning($"{debugFunction} inject {name} {counter} time(s) (Expected {expectedCounter.Value}). Probably not an error but be warned");
      }
    }

  }

  internal class InstructionSequence {

    public static ManualLogSource logger => Plugin.logger;

    List<Func<CodeInstruction, bool>> seq;
    string name;
    string extraErrorMessage;
    int stage = 0;
    bool completed = false;
    bool single;

    public InstructionSequence(string name, bool single = true, string extraErrorMessage = default(string)){
      this.name = name;
      this.single = single;
      this.extraErrorMessage = extraErrorMessage;
      seq = new List<Func<CodeInstruction, bool>>();
    }

    public void Add(Func<CodeInstruction, bool> next){
      seq.Add(next);
    }

    public void AddBasic(OpCode opcode){
      seq.Add((i) => i.opcode == opcode);
    }

    public void AddBasic(OpCode opcode, object operand){
      seq.Add((i) => i.opcode == opcode && i.operand == operand);
    }

    public void AddBasicLocal(OpCode opcode, int operand){
      seq.Add((i) => i.opcode == opcode && (i.operand as LocalBuilder).LocalIndex == operand);
    }

    public void AddOperandTypeCheck(OpCode opcode, Type operandType){
      seq.Add((i) => {
        var fieldInfo = i.operand as FieldInfo;
        if (i.opcode == opcode && fieldInfo != null) {
          return fieldInfo.FieldType == operandType;
        }
        return false;
      });
    }


    public void AddBasicWithAlternateMethodName(OpCode opcode, object operand, string methodName){
      seq.Add((i) => {
        if (i.opcode == opcode && i.operand == operand) return true;

        var mth = i.operand as MethodInfo;
        if (mth != null && mth.Name == methodName) return true;
        
        return false;
      });

    }

    public void AddSpecial(OpCode opcode, Func<CodeInstruction, bool> extra){
      seq.Add((i) => i.opcode == opcode && extra.Invoke(i));
    }

    public void AddQuickInjection(MethodInfo methodInfo){
      
    }

    public bool VerifyStage(CodeInstruction current){
      var s = seq[stage];
      if (s.Invoke(current)) {
        //Plugin.logger.LogInfo($"{name}({stage}): current.ToString()");
        stage++;
      } else {
        stage = 0;
      }

      if (stage >= seq.Count){

        if (completed && single){
          throw new Exception($"Found multiple valid {name} instructions"); 
        }

        stage = 0;
        completed = true;
        return true;
      }

      return false;
    }

    public void ReportComplete(){
      if (completed == false){
        var errorM = string.IsNullOrWhiteSpace(extraErrorMessage) ? "BIG PROBLEM!" : extraErrorMessage;
        logger.LogError($"HarmonyTranspiler for {name} has failed. {errorM}");
      }
    }

  }

  internal class TranspilerUtilities {

    public static IEnumerable<CodeInstruction> InjectMethod(IEnumerable<CodeInstruction> instructions, InjectionDictionary injection, string debugFunction, int? expectedCounter = default){
      var targets = injection.instructions;
      var codeInjections = injection.injections;
      injection.ResetCounter();

      foreach(var i in instructions){
        foreach(var t in targets){
          if (i.opcode == t.opcode && i.operand == t.operand){
            yield return i;
            foreach(var c in codeInjections) yield return c;
            injection.AddCounter();
            goto GoNext;
          }
        }
        yield return i;

        GoNext:;
      }

      injection.Report(debugFunction, expectedCounter);
    }

    public static bool IsInstructionNearFloatValue(CodeInstruction instruction, float value){
      return Mathf.Abs((float)instruction.operand - value) < 0.1f;
    }

    public static void PrintInstructions(IEnumerable<CodeInstruction> instructions) {
      foreach(var i in instructions){
        var opString = i.opcode.ToString();
        var objString = i.operand != null ? i.operand.ToString() : "NULL";
        Plugin.logger.LogInfo($"{opString}: {objString}");
      }
    }

  }
}
