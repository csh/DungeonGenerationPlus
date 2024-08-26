using DunGen;
using DunGenPlus.DevTools.UIElements;
using DunGenPlus.DevTools.UIElements.Collections;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DunGenPlus.DevTools.Panels {
  internal class MainPanel : BasePanel {

    public static MainPanel Instance { get; internal set; }

    internal IntInputField seedInputField;
    internal TextUIElement lengthMultiplierField;

    internal ExtendedLevel[] levels;
    internal IEnumerable<string> levelOptions;

    public override void AwakeCall(){
      Instance = this;

      GetAllLevels();

      var gen = dungeon.Generator;
      var parentTransform = mainGameObject.transform;

      manager.CreateHeaderUIField(parentTransform, "Dungeon Generator");
      seedInputField = manager.CreateIntInputField(parentTransform, "Seed", gen.Seed, SetSeed);
      manager.CreateBoolInputField(parentTransform, "Randomize Seed", gen.ShouldRandomizeSeed, SetRandomSeed);
      manager.CreateSpaceUIField(parentTransform);

      manager.CreateIntInputField(parentTransform, "Max Attempts", new IntParameter(gen.MaxAttemptCount, 0, 500, 0), SetMaxAttempts);
      manager.CreateSpaceUIField(parentTransform);

      manager.CreateBoolInputField(parentTransform, "Generate Async", gen.GenerateAsynchronously, SetGenerateAsync);
      manager.CreateFloatInputField(parentTransform, "Max Async (ms)", new FloatParameter(gen.MaxAsyncFrameMilliseconds, 0f, float.MaxValue), SetMaxAsync);
      manager.CreateFloatInputField(parentTransform, "Pause Between Rooms", new FloatParameter(gen.PauseBetweenRooms, 0f, float.MaxValue), SetPauseBetweenRooms);
      manager.CreateSpaceUIField(parentTransform);

      manager.CreateHeaderUIField(parentTransform, "Levels");
      manager.CreateLevelOptionsUIField(parentTransform, "Level", 0, SetLevel);
      lengthMultiplierField = manager.CreateTextUIField(parentTransform, "Length Multiplier");
      SetLevel(levels[0]);
    }

    public void SetSeed(int value) {
      dungeon.Generator.Seed = value;
    }

    public void SetRandomSeed(bool state) {
      dungeon.Generator.ShouldRandomizeSeed = state;
    }

    public void SetMaxAttempts(int value) {
      dungeon.Generator.MaxAttemptCount = value;
    }

    public void SetGenerateAsync(bool state) {
      dungeon.Generator.GenerateAsynchronously = state;
    }

    public void SetMaxAsync(float value) {
      dungeon.Generator.MaxAsyncFrameMilliseconds = value;
    }

    public void SetPauseBetweenRooms(float value) {
      dungeon.Generator.PauseBetweenRooms = value;
    }

    private void GetAllLevels(){
      levels = LethalLevelLoader.PatchedContent.ExtendedLevels.ToArray();
      levelOptions = levels.Select(l => l.NumberlessPlanetName);
    }

    public void SetLevel(ExtendedLevel level){
      var currentLevelLengthMultlpier = GetLevelMultiplier(level);
      dungeon.Generator.LengthMultiplier = currentLevelLengthMultlpier;
      manager.UpdateDungeonBounds();
      lengthMultiplierField.SetText($"Length multiplier: {currentLevelLengthMultlpier.ToString("F2")}");
    }

    private float GetLevelMultiplier(ExtendedLevel level){
      var roundManager = RoundManager.Instance;
      if (roundManager == null) {
        Plugin.logger.LogError("RoundManager somehow null. Can't set level length multiplier");
        return 1f;
      }

      return roundManager.mapSizeMultiplier * level.SelectableLevel.factorySizeMultiplier; 
    }

  }
}
