using DunGenPlus.DevTools.HoverUI;
using DunGenPlus.DevTools.UIElements.Collections;
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

    public void SetupBase(TitleParameter titleParameter) {
      title = titleParameter.text;
      SetText(title);
      SetHoverText(titleParameter.hoverText);

      layoutOffset = titleParameter.offset;
      if (layoutElement) {
        layoutElement.minWidth -= layoutOffset;
      }
      
    }

    public void SetText(string value) {
      titleTextMesh.text = value;
    }

    public void SetHoverText(string value){
      var hoverChild = GetComponentInChildren<HoverUIChild>();
      if (hoverChild) {
        hoverChild.hoverText = value;
      }
    }

  }
}
