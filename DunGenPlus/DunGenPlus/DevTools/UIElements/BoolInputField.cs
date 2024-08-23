using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

namespace DunGenPlus.DevTools.UIElements {
  internal class BoolInputField : BaseInputField<bool> {

    public Toggle toggle;

    public override void SetupInputField(string title,  float offset, bool baseValue, Action<bool> setAction, bool defaultValue) {
      base.SetupInputField(title, offset, baseValue, setAction, defaultValue);

      toggle.onValueChanged.AddListener((t) => SetValue(setAction, t));
      Set(baseValue);
    }

    private void SetValue(Action<bool> setAction, bool state) {
      Plugin.logger.LogInfo($"Setting {title} to {state}");
      setAction.Invoke(state);
    }

    public override void Set(bool state){
      toggle.isOn = state;
    }

  }
}
