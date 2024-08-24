using DunGen;
using DunGenPlus.Collections;
using DunGenPlus.DevTools.Panels;
using DunGenPlus.DevTools.UIElements;
using DunGenPlus.DevTools.UIElements.Collections;
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

    public TextUIElement CreateHeaderUIField(Transform parentTransform, TitleParameter titleParameter) {
      var gameObject = Instantiate(headerUIPrefab, parentTransform);
      var field = gameObject.GetComponent<TextUIElement>();
      field.SetupBase(titleParameter);
      return field;
    }

    public TextUIElement CreateTextUIField(Transform parentTransform, TitleParameter titleParameter) {
      var gameObject = Instantiate(textUIPrefab, parentTransform);
      var field = gameObject.GetComponent<TextUIElement>();
      field.SetupBase(titleParameter);
      return field;
    }

    public void CreateSpaceUIField(Transform parentTransform) {
      Instantiate(spaceUIPrefab, parentTransform);
    }

    public Transform CreateVerticalLayoutUIField(Transform parentTransform){
      return Instantiate(verticalLayoutUIPrefab, parentTransform).transform;
    }

    public IntInputField CreateIntInputField(Transform parentTransform, TitleParameter titleParameter, IntParameter intParameter, Action<int> setAction){
      var gameObject = Instantiate(intInputFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<IntInputField>();
      field.SetupInputField(titleParameter, intParameter, setAction);
      return field;
    }

    public FloatInputField CreateFloatInputField(Transform parentTransform, TitleParameter titleParameter, FloatParameter floatParameter, Action<float> setAction){
      var gameObject = Instantiate(floatInputFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<FloatInputField>();
      field.SetupInputField(titleParameter, floatParameter, setAction);
      return field;
    }

    public BoolInputField CreateBoolInputField(Transform parentTransform, TitleParameter titleParameter, bool baseValue, Action<bool> setAction){
      var gameObject = Instantiate(boolInputFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<BoolInputField>();
      field.SetupInputField(titleParameter, baseValue, setAction);
      return field;
    }

    public StringInputField CreateStringInputField(Transform parentTransform, TitleParameter titleParameter, string baseValue, Action<string> setAction){
      var gameObject = Instantiate(stringInputFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<StringInputField>();
      field.SetupInputField(titleParameter, baseValue, setAction);
      return field;
    }

    public Vector3InputField CreateVector3InputField(Transform parentTransform, TitleParameter titleParameter, Vector3 baseValue, Action<Vector3> setAction){
      var gameObject = Instantiate(vector3InputFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<Vector3InputField>();
      field.SetupInputField(titleParameter, baseValue, setAction);
      return field;
    }

    public IntSliderField CreateIntSliderField(Transform parentTransform, TitleParameter titleParameter, IntParameter intParameter, Action<int> setAction){
      var gameObject = Instantiate(intSliderFieldPrefab, parentTransform);
      var field = gameObject.GetComponent<IntSliderField>();
      field.SetupInputField(titleParameter, intParameter, setAction);
      return field;
    }

    public ListUIElement CreateListUIField<T>(Transform parentTransform, TitleParameter titleParameter, List<T> list){
      var gameObject = Instantiate(listUIPrefab, parentTransform);
      var field = gameObject.GetComponent<ListUIElement>();
      field.SetupList(titleParameter, list);
      return field;
    }


    public DropdownInputField CreateOptionsUIField<T>(Transform parentTransform, TitleParameter titleParameter, int baseValue, Action<T> setAction, Func<int, T> convertIndex, IEnumerable<string> options){
      var gameObject = Instantiate(optionsUIPrefab, parentTransform);
      var field = gameObject.GetComponent<DropdownInputField>();
      field.SetupDropdown(titleParameter, baseValue, setAction, convertIndex, options);
      return field;
    }

    public DropdownInputField CreateLevelOptionsUIField(Transform parentTransform, TitleParameter titleParameter, int baseValue, Action<ExtendedLevel> setAction){
      var mainPanel = MainPanel.Instance;
      return CreateOptionsUIField(parentTransform, titleParameter, baseValue, setAction, (i) => mainPanel.levels[i], mainPanel.levelOptions);
    }

    public DropdownInputField CreateTileOptionsUIField(Transform parentTransform, TitleParameter titleParameter, int baseValue, Action<GameObject> setAction){
      var assetCache = DunGenPlusPanel.Instance.selectedAssetCache;
      return CreateOptionsUIField(parentTransform, titleParameter, baseValue, setAction, (i) => assetCache.tiles.list[i].Item, assetCache.tiles.options);
    }

    public DropdownInputField CreateTileSetsOptionsUIField(Transform parentTransform, TitleParameter titleParameter, int baseValue, Action<TileSet> setAction){
      var assetCache = DunGenPlusPanel.Instance.selectedAssetCache;
      return CreateOptionsUIField(parentTransform, titleParameter, baseValue, setAction, (i) => assetCache.tileSets.list[i].Item, assetCache.tileSets.options);
    }

    public DropdownInputField CreateArchetypeOptionsUIField(Transform parentTransform, TitleParameter titleParameter, int baseValue, Action<DungeonArchetype> setAction){
      var assetCache = DunGenPlusPanel.Instance.selectedAssetCache;
      return CreateOptionsUIField(parentTransform, titleParameter, baseValue, setAction, (i) => assetCache.archetypes.list[i].Item, assetCache.archetypes.options);
    }

    public DropdownInputField CreateCopyNodeBehaviourOptionsUIField(Transform parentTransform, TitleParameter titleParameter, int baseValue, Action<DunGenExtenderProperties.CopyNodeBehaviour> setAction){
      var options = Enum.GetNames(typeof(DunGenExtenderProperties.CopyNodeBehaviour));
      return CreateOptionsUIField(parentTransform, titleParameter, baseValue, setAction, (i) => (DunGenExtenderProperties.CopyNodeBehaviour)i, options);
    }

    public DropdownInputField CreateAnimationCurveOptionsUIField(Transform parentTransform, TitleParameter titleParameter, AnimationCurve baseValue, Action<AnimationCurve> setAction){
      var result = CreateAnimationCurves(baseValue);
      var curves = result.animationCurves;
      var options = result.options;
      setAction.Invoke(curves[0]);
      return CreateOptionsUIField(parentTransform, titleParameter, 0, setAction, (i) => curves[i], options);
    }

    private (List<AnimationCurve> animationCurves, List<string> options) CreateAnimationCurves(AnimationCurve custom){
      var curves = new List<AnimationCurve>();
      var options = new List<string>();
      if (custom != null){
        curves.Add(custom);
        options.Add("Custom");
      }

      curves.Add(AnimationCurve.Constant(0f, 1f, 1f));
      options.Add("Constant 1");

      curves.Add(AnimationCurve.Linear(0f, 0f, 1f, 1f));
      options.Add("Linear 0-1");

      curves.Add(AnimationCurve.Linear(1f, 1f, 0f, 0f));
      options.Add("Linear 1-0");

      curves.Add(AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
      options.Add("EaseInOut 0-1");

      curves.Add(AnimationCurve.EaseInOut(1f, 1f, 0f, 0f));
      options.Add("EaseInOut 1-0");

      return (curves, options);
    }

  }
}
