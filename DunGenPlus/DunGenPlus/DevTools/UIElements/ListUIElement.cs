using DunGen;
using DunGenPlus.Collections;
using DunGenPlus.DevTools.Panels;
using LethalLevelLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace DunGenPlus.DevTools.UIElements {
  internal class ListUIElement : BaseUIElement {

    public GameObject templatePrefab;
    public Transform listTransform;

    internal IList list;
    internal Type listType;

    public void SetupList<T>(string titleText, float offset, List<T> list) {
      SetupBase(titleText, offset);

      this.list = list;
      listType = typeof(T);
      for(var i = 0; i < list.Count; ++i) {
        CreateEntry(i);
      }
    }

    public void AddElement() {
      object item = null;
      if (listType == typeof(DungeonArchetype)) {
        item = null;
      } else if (listType == typeof(NodeArchetype)) {
        item = new NodeArchetype();
      }

      list.Add(item);
      CreateEntry(list.Count - 1);
    }

    public void RemoveElement(){
      if (list.Count == 0) return;
      list.RemoveAt(list.Count - 1);
      Destroy(listTransform.GetChild(listTransform.childCount - 1).gameObject);
    }

    public void CreateEntry(int index){
      var copy = CreateCopy(index);
      var copyParentTransform = copy.transform.Find("Items");

      if (listType == typeof(DungeonArchetype)){
        var entry = (DungeonArchetype)list[index];
        var baseValue = DunGenPlusPanel.Instance.selectedAssetCache.archetypes.dictionary[entry];
        DevDebugManager.Instance.CreateArchetypeOptionsUIField(copyParentTransform, "Archetype", layoutOffset + 24f, baseValue, (t) => list[index] = t);
      }

      else if (listType == typeof(NodeArchetype)) {
        var entry = (NodeArchetype)list[index];
        DevDebugManager.Instance.CreateStringInputField(copyParentTransform, "Label", layoutOffset + 24f, entry.label, (t) => entry.label = t);
        DevDebugManager.Instance.CreateListUIField(copyParentTransform, "Archetypes", layoutOffset + 24f, entry.archetypes);
      }

      copy.SetActive(true);
    }

    public GameObject CreateCopy(int index){
      var copy = Instantiate(templatePrefab, listTransform);
      copy.transform.Find("Element").GetComponent<TextMeshProUGUI>().text = $"Element {index}";
      return copy;
    }

  }
}
