using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace DunGenPlus.DevTools.UIElements
{
  internal class FloatInputField : BaseInputField<float> {

    public TMP_InputField inputField;
    internal float defaultValue = 0f;

    public override void SetupInputField(string title, float offset, float baseValue, Action<float> setAction , float defaultValue) {
      base.SetupInputField(title, offset, baseValue, setAction, defaultValue);
      this.defaultValue = defaultValue;

      inputField.onValueChanged.AddListener((t) => SetValue(setAction, t));
      Set(baseValue);
    }

    private void SetValue(Action<float> setAction, string text) {
      Plugin.logger.LogInfo($"Setting {title} to {text}");
      setAction.Invoke(ParseTextFloat(text, defaultValue));
    }

    public override void Set(float value){
      inputField.text = value.ToString();
    }
  }
}
