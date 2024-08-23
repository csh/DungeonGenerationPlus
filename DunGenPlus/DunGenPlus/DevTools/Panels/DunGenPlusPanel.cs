using DunGen;
using DunGen.Graph;
using DunGenPlus.Collections;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DunGenPlus.DevTools.UIElements;

namespace DunGenPlus.DevTools.Panels {
  internal class DunGenPlusPanel : BasePanel {

    public static DunGenPlusPanel Instance { get; private set; }

    internal DungeonFlow previousDungeonFlow;
    internal DunGenExtender selectedExtenderer;
    internal DungeonFlowCacheAssets selectedAssetCache;

    [Header("Panel References")]
    public GameObject createGameObject;
    public GameObject selectedGameObject;

    [Header("Dungeon Bounds Helper")]
    public GameObject dungeonBoundsHelperGameObject;

    [Header("Selected Panel References")]
    public Toggle activateDunGenPlusToggle;
    
    private GameObject mainPathParentGameobject;
    private GameObject dungeonBoundsParentGameobject;
    private GameObject archetypesNodesParentGameobject;

    public class DungeonFlowCacheAssets {
      public DunGenExtenderProperties originalProperties;

      public struct Collection<T> {
        public List<T> list;
        public Dictionary<T, int> dictionary;
        public IEnumerable<string> options;

        public Collection(List<T> list) {
          this.list = list;

          dictionary = new Dictionary<T, int>();
          for(var i = 0;  i < list.Count; i++) {
            dictionary.Add(list[i], i);
          }

          options = list.Select(l => l.ToString());
        }
      }

      public Collection<NullObject<TileSet>> tileSets;
      public Collection<NullObject<GameObject>> tiles;
      public Collection<NullObject<DungeonArchetype>> archetypes;

      public DungeonFlowCacheAssets(DunGenExtender extender){
        originalProperties = extender.Properties.Copy();
        
        var tileSetsHashSet = new HashSet<NullObject<TileSet>>() { new NullObject<TileSet>(null) };
        var tilesHashSet = new HashSet<NullObject<GameObject>>() { new NullObject<GameObject>(null) };
        var archetypesHashSet = new HashSet<NullObject<DungeonArchetype>>() { new NullObject<DungeonArchetype>(null) };

        foreach(var t in extender.DungeonFlow.Nodes) {
          var label = t.Label.ToLowerInvariant();
          if (label == "lchc gate" || label == "goal"){
            foreach(var n in t.TileSets.SelectMany(x => x.TileWeights.Weights)) {
              n.Value.GetComponent<Tile>().RepeatMode = TileRepeatMode.Allow;
            }
          }

        }

        foreach(var t in extender.DungeonFlow.Nodes.SelectMany(n => n.TileSets)) {
          tileSetsHashSet.Add(t);
          foreach(var x in t.TileWeights.Weights) {
            tilesHashSet.Add(x.Value);
          }
        }
        foreach(var a in extender.DungeonFlow.Lines.SelectMany(l => l.DungeonArchetypes)) {
          archetypesHashSet.Add(a);
          foreach(var t in a.TileSets) {
            tileSetsHashSet.Add(t);
            foreach(var x in t.TileWeights.Weights) {
              tilesHashSet.Add(x.Value);
            }
          }
        }

        foreach(var n in extender.Properties.NormalNodeArchetypes) {
          foreach(var a in n.archetypes){
            archetypesHashSet.Add(a);
          }
        }

        tileSets = new Collection<NullObject<TileSet>>(tileSetsHashSet.ToList());
        tiles = new Collection<NullObject<GameObject>>(tilesHashSet.ToList());
        archetypes = new Collection<NullObject<DungeonArchetype>>(archetypesHashSet.ToList());
      }
    }

    public Dictionary<DungeonFlow, DungeonFlowCacheAssets> cacheDictionary = new Dictionary<DungeonFlow, DungeonFlowCacheAssets>();

    public override void AwakeCall() {
      Instance = this;

      dungeonBoundsHelperGameObject.SetActive(false);
    }

    public override void SetPanelVisibility(bool visible) {
      base.SetPanelVisibility(visible);

      if (visible) UpdatePanel();
    }

    public void CreateDunGenPlusExtenderer(){
      var asset = API.CreateDunGenExtender(selectedDungeonFlow);
      selectedDungeonFlow.TileInjectionRules = new List<TileInjectionRule>();
      API.AddDunGenExtender(asset);
      SetPanelVisibility(true);
      UpdatePanel();
    }

    public void UpdatePanel(){
      if (previousDungeonFlow == selectedDungeonFlow) return;

      var hasAsset = API.ContainsDungeonFlow(selectedDungeonFlow);
      selectedGameObject.SetActive(hasAsset);
      createGameObject.SetActive(!hasAsset);

      ClearPanel();
      if (hasAsset) {
        SetupPanel(); 
      } else {
        previousDungeonFlow = null;
        selectedExtenderer = null;
        selectedAssetCache = null;
        dungeonBoundsHelperGameObject.SetActive(false);
      }
    }

    public void SetupPanel() {
      var dungeonFlow = selectedDungeonFlow;
      var extender = API.GetDunGenExtender(dungeonFlow);
      if (!cacheDictionary.TryGetValue(dungeonFlow, out var cache)) {
        cache = new DungeonFlowCacheAssets(extender);
        cacheDictionary.Add(dungeonFlow, cache);
      }

      previousDungeonFlow = dungeonFlow;
      selectedExtenderer = extender;
      selectedAssetCache = cache;

      var parentTransform = selectedGameObject.transform;
      var properties = selectedExtenderer.Properties;
      manager.CreateBoolInputField(parentTransform, "Activate DunGenPlus", 0f, selectedExtenderer.Active, SetActivateDunGenPlus);
      manager.CreateSpaceUIField(parentTransform);

      var mainPathTransform = manager.CreateVerticalLayoutUIField(parentTransform);
      mainPathParentGameobject = mainPathTransform.gameObject;
      manager.CreateHeaderUIField(parentTransform, "Main Path", 0f);
      manager.CreateIntSliderField(parentTransform, "Main Path Count", 0f, properties.MainPathCount, SetMainPathCount);
      mainPathTransform.SetAsLastSibling();
      manager.CreateTileOptionsUIField(mainPathTransform, "Main Room Tile Prefab", 0f, selectedAssetCache.tiles.dictionary[properties.MainRoomTilePrefab], SetMainRoom);
      manager.CreateCopyNodeBehaviourOptionsUIField(mainPathTransform, "Copy Node Behaviour", 0f, (int)properties.MainPathCopyNodeBehaviour, SetCopyNodeBehaviour);
      manager.CreateSpaceUIField(parentTransform);

      var dungeonBoundsTransform = manager.CreateVerticalLayoutUIField(parentTransform);
      dungeonBoundsParentGameobject = dungeonBoundsTransform.gameObject;
      manager.CreateHeaderUIField(parentTransform, "Dungeon Bounds", 0f);
      manager.CreateBoolInputField(parentTransform, "Use Dungeon Bounds", 0f, properties.UseDungeonBounds, SetUseDungeonBounds);
      dungeonBoundsTransform.SetAsLastSibling();
      manager.CreateVector3InputField(dungeonBoundsTransform, "Size Base", 0f, properties.DungeonSizeBase, SetDungeonBoundsSizeBase);
      manager.CreateVector3InputField(dungeonBoundsTransform, "Size Factor", 0f, properties.DungeonSizeFactor, SetDungeonBoundsSizeFactor);
      manager.CreateVector3InputField(dungeonBoundsTransform, "Position Offset", 0f, properties.DungeonPositionOffset, SetDungeonBoundsPosOffset);
      manager.CreateVector3InputField(dungeonBoundsTransform, "Position Pivot", 0f, properties.DungeonPositionPivot, SetDungeonBoundsPosPivot);
      manager.CreateSpaceUIField(parentTransform);

      var archetypesTransform = manager.CreateVerticalLayoutUIField(parentTransform);
      archetypesNodesParentGameobject = archetypesTransform.gameObject;
      manager.CreateHeaderUIField(parentTransform, "Archetypes Normal Nodes", 0f);
      manager.CreateBoolInputField(parentTransform, "Add Archetypes", 0f, properties.AddArchetypesToNormalNodes, SetAddArchetypes);
      archetypesTransform.SetAsLastSibling();
      manager.CreateListUIField(archetypesTransform, "Normal Node Archetypes", 0f, properties.NormalNodeArchetypes);
      manager.CreateSpaceUIField(parentTransform);

      dungeonBoundsHelperGameObject.SetActive(selectedExtenderer.Properties.UseDungeonBounds);
      UpdateDungeonBoundsHelper();
    }

    public void ClearPanel(){
      manager.ClearTransformChildren(selectedGameObject.transform);
    }

    public void SetActivateDunGenPlus(bool state){
      selectedExtenderer.Active = state;
    }

    public void SetMainPathCount(int value) {
      selectedExtenderer.Properties.MainPathCount = value;
      mainPathParentGameobject.SetActive(value > 1);
    }

    public void SetMainRoom(GameObject value) {
      selectedExtenderer.Properties.MainRoomTilePrefab = value;
    }

    public void SetCopyNodeBehaviour(DunGenExtenderProperties.CopyNodeBehaviour value) {
      selectedExtenderer.Properties.MainPathCopyNodeBehaviour = value;
    }

    public void SetUseDungeonBounds(bool state){
      selectedExtenderer.Properties.UseDungeonBounds = state;
      dungeonBoundsHelperGameObject.SetActive(state);
      dungeonBoundsParentGameobject.SetActive(state);
    }

    public void UpdateDungeonBoundsHelper(){
      if (selectedExtenderer == null) return;

      var t = dungeonBoundsHelperGameObject.transform;
      var result = selectedExtenderer.Properties.GetDungeonBounds(dungeon.Generator.LengthMultiplier);
      t.localPosition = result.center;
      t.localScale = result.size;
    }

    public void SetDungeonBoundsSizeBase(Vector3 value) {
      selectedExtenderer.Properties.DungeonSizeBase = value;
      UpdateDungeonBoundsHelper();
    }

    public void SetDungeonBoundsSizeFactor(Vector3 value) {
      selectedExtenderer.Properties.DungeonSizeFactor = value;
      UpdateDungeonBoundsHelper();
    }

    public void SetDungeonBoundsPosOffset(Vector3 value) {
      selectedExtenderer.Properties.DungeonPositionOffset = value;
      UpdateDungeonBoundsHelper();
    }

    public void SetDungeonBoundsPosPivot(Vector3 value) {
      selectedExtenderer.Properties.DungeonPositionPivot = value;
      UpdateDungeonBoundsHelper();
    }

    public void SetAddArchetypes(bool state){
      selectedExtenderer.Properties.AddArchetypesToNormalNodes = state;
      archetypesNodesParentGameobject.SetActive(state);
    }

  }
}
