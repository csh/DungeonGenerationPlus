using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using DunGenPlus;

namespace DunGenPlusEditor {
  [CustomPropertyDrawer(typeof(PropertyOverride<>), true)]
  public class PropertyOverridePropertyDrawer : PropertyDrawer {

    public override VisualElement CreatePropertyGUI(SerializedProperty property) {

      var container = new VisualElement();

      var box = new Box();
      box.style.paddingBottom = 4f;
      box.style.paddingLeft = 4f;
      box.style.paddingRight = 4f;
      box.style.paddingTop = 4f;
      box.style.marginBottom = 8f;

      var label = new Label(property.displayName);
      var weight = label.style.unityFontStyleAndWeight;
      weight.value = UnityEngine.FontStyle.Bold;
      label.style.unityFontStyleAndWeight = weight;
      box.Add(label);

      var overrideProperty = property.FindPropertyRelative("Override");
      var valueProperty = property.FindPropertyRelative("Value");

      var overrideItem = new PropertyField(overrideProperty);
      overrideItem.style.marginLeft = 8f;
      var valueItem = new PropertyField(valueProperty);
      valueItem.style.marginLeft = 8f;
      var valueDefaultItem = new Label("Using DungeonFlow's corresponding values");
      valueDefaultItem.style.marginLeft = 11f;

      void SetDisplayState(bool state){
        valueItem.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
        valueDefaultItem.style.display = !state ? DisplayStyle.Flex : DisplayStyle.None;
      }
      SetDisplayState(overrideProperty.boolValue);
      overrideItem.RegisterValueChangeCallback(evt => SetDisplayState(evt.changedProperty.boolValue));

      box.Add(overrideItem);
      box.Add(valueItem);
      box.Add(valueDefaultItem);

      container.Add(box);

      return container;
    }

  }
}
