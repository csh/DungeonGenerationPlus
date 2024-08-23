using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DunGenPlus.DevTools.UIElements {
  internal abstract class BaseUIElement : MonoBehaviour {

    public TextMeshProUGUI titleTextMesh;
    internal string title;

    public LayoutElement layoutElement;
    internal float layoutOffset;

    public void SetupBase(string titleText, float offset) {
      title = titleText;
      SetText(title);

      layoutOffset = offset;
      if (layoutElement) {
        layoutElement.minWidth -= layoutOffset;
      }
      
    }

    public void SetText(string value) {
      titleTextMesh.text = value;
    }


  }
}
