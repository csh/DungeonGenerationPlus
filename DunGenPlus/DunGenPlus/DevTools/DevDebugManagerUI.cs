using DunGen;
using DunGenPlus.Collections;
using DunGenPlus.DevTools.Panels;
using DunGenPlus.DevTools.UIElements;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

namespace DunGenPlus.DevTools {

  internal partial class DevDebugManager : MonoBehaviour {

    [Header("UI Prefabs")]
    [Header("UI")]
    public GameObject headerUIPrefab;
    public GameObject textUIPrefab;
    public GameObject spaceUIPrefab;
    public GameObject verticalLayoutUIPrefab;

    [Header("Input Fields")]
    public GameObject intInputFieldPrefab;
    public GameObject floatInputFieldPrefab;
    public GameObject boolInputFieldPrefab;
    public GameObject stringInputFieldPrefab;
    public GameObject vector3InputFieldPrefab;
    public GameObject intSliderFieldPrefab;

    [Header("Special Fields")]
    public GameObject listUIPrefab;
    public GameObject optionsUIPrefab;

    public TextUIElement CreateHeaderUIField(Transform parentTransform, string title, float offset) {
      var gameObject = Instantiate(headerUIPrefab, parentTransform);
      var field = gameObject.GetComponent<TextUIElement>();
      field.SetupBase(title, offset);
      return field;
    }

    public TextUIElement CreateTextUIField(Transform parentTransform, string title, float offset) {
      var gameObject = Instantiate(textUIPrefab, parentTransform);
      var field = gameObject.GetComponent<TextUIElement>();
      field.SetupBase(title, offset);
      return field;
    }

    public void CreateSpaceUIField(Transform parentTransform) {
      Instantiate(spaceUIPrefab, parentTransform);
    }

    public Transform CreateVerticalLayoutUIField(Transform parentTransform){
      return Instantiate(verticalLayoutUIPrefab, parentTransform).transform;
    }

    public IntInputField CreateIntInputField(Transform parentTransform, string title, float offset, int baseValue, Action<int> setAction, int defaultValue = 0){
      var gameObject = Instantiate(intInputFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<IntInputField>();
      field.SetupInputField(title, offset, baseValue, setAction, defaultValue);
      return field;
    }

    public FloatInputField CreateFloatInputField(Transform parentTransform, string title, float offset, float baseValue, Action<float> setAction, float defaultValue = 0f){
      var gameObject = Instantiate(floatInputFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<FloatInputField>();
      field.SetupInputField(title, offset, baseValue, setAction, defaultValue);
      return field;
    }

    public BoolInputField CreateBoolInputField(Transform parentTransform, string title, float offset, bool baseValue, Action<bool> setAction){
      var gameObject = Instantiate(boolInputFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<BoolInputField>();
      field.SetupInputField(title, offset, baseValue, setAction, false);
      return field;
    }

    public StringInputField CreateStringInputField(Transform parentTransform, string title, float offset, string baseValue, Action<string> setAction){
      var gameObject = Instantiate(stringInputFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<StringInputField>();
      field.SetupInputField(title, offset, baseValue, setAction, string.Empty);
      return field;
    }

    public Vector3InputField CreateVector3InputField(Transform parentTransform, string title, float offset, Vector3 baseValue, Action<Vector3> setAction){
      var gameObject = Instantiate(vector3InputFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<Vector3InputField>();
      field.SetupInputField(title, offset, baseValue, setAction, Vector3.zero);
      return field;
    }

    public IntSliderField CreateIntSliderField(Transform parentTransform, string title, float offset, int baseValue, Action<int> setAction, int defaultValue = 0){
      var gameObject = Instantiate(intSliderFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<IntSliderField>();
      field.SetupInputField(title, offset, baseValue, setAction, defaultValue);
      return field;
    }

    public ListUIElement CreateListUIField<T>(Transform parentTransform, string title, float offset, List<T> list){
      var gameObject = Instantiate(listUIPrefab, parentTransform);
      var field = gameObject.GetComponent<ListUIElement>();
      field.SetupList(title, offset, list);
      return field;
    }


    public DropdownInputField CreateOptionsUIField<T>(Transform parentTransform, string title, float offset, int baseValue, Action<T> setAction, Func<int, T> convertIndex, IEnumerable<string> options){
      var gameObject = Instantiate(optionsUIPrefab, parentTransform);
      var field = gameObject.GetComponent<DropdownInputField>();
      field.SetupDropdown(title, offset, baseValue, setAction, convertIndex, options);
      return field;
    }

    public DropdownInputField CreateLevelOptionsUIField(Transform parentTransform, string title, float offset, int baseValue, Action<ExtendedLevel> setAction){
      var mainPanel = MainPanel.Instance;
      return CreateOptionsUIField(parentTransform, title, offset, baseValue, setAction, (i) => mainPanel.levels[i], mainPanel.levelOptions);
    }

    public DropdownInputField CreateTileOptionsUIField(Transform parentTransform, string title, float offset, int baseValue, Action<GameObject> setAction){
      var assetCache = DunGenPlusPanel.Instance.selectedAssetCache;
      return CreateOptionsUIField(parentTransform, title, offset, baseValue, setAction, (i) => assetCache.tiles.list[i].Item, assetCache.tiles.options);
    }

    public DropdownInputField CreateArchetypeOptionsUIField(Transform parentTransform, string title, float offset, int baseValue, Action<DungeonArchetype> setAction){
      var assetCache = DunGenPlusPanel.Instance.selectedAssetCache;
      return CreateOptionsUIField(parentTransform, title, offset, baseValue, setAction, (i) => assetCache.archetypes.list[i].Item, assetCache.archetypes.options);
    }

    public DropdownInputField CreateCopyNodeBehaviourOptionsUIField(Transform parentTransform, string title, float offset, int baseValue, Action<DunGenExtenderProperties.CopyNodeBehaviour> setAction){
      var options = Enum.GetNames(typeof(DunGenExtenderProperties.CopyNodeBehaviour));
      return CreateOptionsUIField(parentTransform, title, offset, baseValue, setAction, (i) => (DunGenExtenderProperties.CopyNodeBehaviour)i, options);
    }

  }
}
