using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

namespace DunGenPlus.DevTools.UIElements {

  internal class IntSliderField : BaseInputField<int> {

    public Slider inputField;
    public TextMeshProUGUI textMesh;
    internal int defaultValue = 0;

    public override void SetupInputField(string title, float offset, int baseValue, Action<int> setAction , int defaultValue) {
      base.SetupInputField(title, offset, baseValue, setAction, defaultValue);
      this.defaultValue = defaultValue;

      inputField.onValueChanged.AddListener((t) => SetValue(setAction, t));
      Set(baseValue);
    }

    private void SetValue(Action<int> setAction, float value) {
      Plugin.logger.LogInfo($"Setting {title} to {value}");
      setAction.Invoke((int)value);
    }

    public override void Set(int value){
      inputField.value = value;
      textMesh.text = value.ToString();
    }

  }
}
