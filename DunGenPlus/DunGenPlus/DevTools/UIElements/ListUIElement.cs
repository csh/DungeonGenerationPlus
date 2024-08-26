using DunGen;
using DunGen.Graph;
using DunGenPlus.Collections;
using DunGenPlus.DevTools.Panels;
using DunGenPlus.DevTools.UIElements.Collections;
using LethalLevelLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DunGenPlus.DevTools.UIElements {
  internal class ListUIElement : BaseUIElement {

    public GameObject templatePrefab;
    public Transform listTransform;

    internal IList list;
    internal Type listType;

    public static readonly Dictionary<Type, ListEntryType> typeDictionary = new Dictionary<Type, ListEntryType>() {
      { typeof(DungeonArchetype), new ListEntryDungeonArchetype() },
      { typeof(TileSet), new ListEntryTileSet() },
      { typeof(NodeArchetype), new ListEntryNodeArchetype() },
      { typeof(ForcedTileSetList), new ListEntryForcedTileSetList() },
      { typeof(TileInjectionRule), new ListEntryTileInjectionRule() },
      { typeof(GraphNode), new ListEntryGraphNode() },
      { typeof(GraphLine), new ListEntryGraphLine() }
    };

    public void SetupList<T>(TitleParameter titleParameter, List<T> list) {
      SetupBase(titleParameter);

      var cValue = Mathf.LerpUnclamped(0.4f, 0.6f, titleParameter.offset / 100f);
      listTransform.GetComponent<Image>().color = new Color(cValue, cValue, cValue, 1f);

      this.list = list;
      listType = typeof(T);
      for(var i = 0; i < list.Count; ++i) {
        CreateEntry(i);
      }
    }

    public void AddElement() {
      object item = null;
      if (!typeDictionary.TryGetValue(listType, out var value)){
        Plugin.logger.LogError($"Type {listType} does not has a defined list UI display");
      }
      item = value.CreateEmptyObject();
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

      if (!typeDictionary.TryGetValue(listType, out var value)){
        Plugin.logger.LogError($"Type {listType} does not has a defined list UI display");
      }
      value.CreateEntry(list, index, copyParentTransform, layoutOffset + 24f);
      copy.SetActive(true);
    }

    public GameObject CreateCopy(int index){
      var copy = Instantiate(templatePrefab, listTransform);
      copy.transform.Find("Element").GetComponent<TextMeshProUGUI>().text = $"Element {index}";
      return copy;
    }



  }
}
