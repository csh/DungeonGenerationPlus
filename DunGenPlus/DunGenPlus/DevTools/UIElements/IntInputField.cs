using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace DunGenPlus.DevTools.UIElements {
  internal class IntInputField : BaseInputField<int> {

    public TMP_InputField inputField;
    internal int defaultValue = 0;

    public override void SetupInputField(string title, float offset, int baseValue, Action<int> setAction , int defaultValue) {
      base.SetupInputField(title, offset, baseValue, setAction, defaultValue);
      this.defaultValue = defaultValue;

      inputField.onValueChanged.AddListener((t) => SetValue(setAction, t));
      Set(baseValue);
    }

    private void SetValue(Action<int> setAction, string text) {
      Plugin.logger.LogInfo($"Setting {title} to {text}");
      setAction.Invoke(ParseTextInt(text, defaultValue));
    }

    public override void Set(int value){
      inputField.text = value.ToString();
    }

  }
}
