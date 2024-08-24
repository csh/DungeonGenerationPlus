using DunGen;
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
      if (listType == typeof(DungeonArchetype) || listType == typeof(TileSet)) {
        item = null;
      } else if (listType == typeof(NodeArchetype)) {
        item = new NodeArchetype();
      } else if (listType == typeof(ForcedTileSetList)){
        var forcedTileset = new ForcedTileSetList(); 
        forcedTileset.DepthWeightScale = null;
        item = forcedTileset;
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
        DevDebugManager.Instance.CreateArchetypeOptionsUIField(copyParentTransform, new TitleParameter("Archetype", layoutOffset + 24f), baseValue, (t) => list[index] = t);
      }

      else if (listType == typeof(TileSet)){
        var entry = (TileSet)list[index];
        var baseValue = DunGenPlusPanel.Instance.selectedAssetCache.tileSets.dictionary[entry];
        DevDebugManager.Instance.CreateTileSetsOptionsUIField(copyParentTransform, new TitleParameter("Tile Set", layoutOffset + 24f), baseValue, (t) => list[index] = t);
      }

      else if (listType == typeof(NodeArchetype)) {
        var entry = (NodeArchetype)list[index];
        DevDebugManager.Instance.CreateStringInputField(copyParentTransform, new TitleParameter("Label", layoutOffset + 24f), entry.label, (t) => entry.label = t);
        DevDebugManager.Instance.CreateListUIField(copyParentTransform, new TitleParameter("Archetypes", layoutOffset + 24f), entry.archetypes);
      }

      else if (listType == typeof(ForcedTileSetList)) {
        var entry = (ForcedTileSetList)list[index];
        DevDebugManager.Instance.CreateFloatInputField(copyParentTransform, new TitleParameter("Main Path Weight", layoutOffset + 24f), entry.MainPathWeight, (t) => entry.MainPathWeight = t);
        DevDebugManager.Instance.CreateFloatInputField(copyParentTransform, new TitleParameter("Branch Path Weight", layoutOffset + 24f), entry.BranchPathWeight, (t) => entry.BranchPathWeight = t);

        // depth is weird cause we have to account for every entry's unique depth curve, even if they don't have one
        DevDebugManager.Instance.CreateAnimationCurveOptionsUIField(copyParentTransform, new TitleParameter("Depth Weight Scale", layoutOffset + 24f), entry.DepthWeightScale, (t) => entry.DepthWeightScale = t);
        DevDebugManager.Instance.CreateListUIField(copyParentTransform, new TitleParameter("Tile Sets", layoutOffset + 24f), entry.Tilesets);
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
