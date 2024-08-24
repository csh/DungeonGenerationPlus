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
using DunGenPlus.DevTools.UIElements.Collections;
using DunGenPlus.DevTools.Panels.Collections;

namespace DunGenPlus.DevTools.Panels {
  internal class DunGenPlusPanel : BasePanel {

    public static DunGenPlusPanel Instance { get; private set; }

    internal DungeonFlow previousDungeonFlow;
    internal DunGenExtender selectedExtenderer;
    internal DungeonFlowCacheAssets selectedAssetCache;

    [Header("Panel References")]
    public GameObject createGameObject;
    public GameObject selectedGameObject;
    public GameObject selectedListGameObject;

    [Header("Dungeon Bounds Helper")]
    public GameObject dungeonBoundsHelperGameObject;

    private GameObject mainPathParentGameobject;
    private GameObject dungeonBoundsParentGameobject;
    private GameObject archetypesNodesParentGameobject;
    private GameObject forcedTilesParentGameobject;
    private GameObject branchLoopBoostParentGameobject;
    private GameObject maxShadowsParentGameobject;

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

      var parentTransform = selectedListGameObject.transform;
      var properties = selectedExtenderer.Properties;
      manager.CreateBoolInputField(parentTransform, "Activate DunGenPlus", selectedExtenderer.Active, SetActivateDunGenPlus);
      manager.CreateSpaceUIField(parentTransform);

      var mainPathTransform = manager.CreateVerticalLayoutUIField(parentTransform);
      mainPathParentGameobject = mainPathTransform.gameObject;
      manager.CreateHeaderUIField(parentTransform, "Main Path");
      manager.CreateIntSliderField(parentTransform, "Main Path Count", new IntParameter(properties.MainPathCount, 1, 10), SetMainPathCount);
      mainPathTransform.SetAsLastSibling();
      manager.CreateTileOptionsUIField(mainPathTransform, "Main Room Tile Prefab", selectedAssetCache.tiles.dictionary[properties.MainRoomTilePrefab], SetMainRoom);
      manager.CreateCopyNodeBehaviourOptionsUIField(mainPathTransform, "Copy Node Behaviour", (int)properties.MainPathCopyNodeBehaviour, SetCopyNodeBehaviour);
      manager.CreateSpaceUIField(parentTransform);

      var dungeonBoundsTransform = manager.CreateVerticalLayoutUIField(parentTransform);
      dungeonBoundsParentGameobject = dungeonBoundsTransform.gameObject;
      manager.CreateHeaderUIField(parentTransform, "Dungeon Bounds");
      manager.CreateBoolInputField(parentTransform, "Use Dungeon Bounds", properties.UseDungeonBounds, SetUseDungeonBounds);
      dungeonBoundsTransform.SetAsLastSibling();
      manager.CreateVector3InputField(dungeonBoundsTransform, "Size Base", properties.DungeonSizeBase, SetDungeonBoundsSizeBase);
      manager.CreateVector3InputField(dungeonBoundsTransform, "Size Factor", properties.DungeonSizeFactor, SetDungeonBoundsSizeFactor);
      manager.CreateVector3InputField(dungeonBoundsTransform, "Position Offset", properties.DungeonPositionOffset, SetDungeonBoundsPosOffset);
      manager.CreateVector3InputField(dungeonBoundsTransform, "Position Pivot", properties.DungeonPositionPivot, SetDungeonBoundsPosPivot);
      manager.CreateSpaceUIField(parentTransform);

      var archetypesTransform = manager.CreateVerticalLayoutUIField(parentTransform);
      archetypesNodesParentGameobject = archetypesTransform.gameObject;
      manager.CreateHeaderUIField(parentTransform, "Archetypes Normal Nodes");
      manager.CreateBoolInputField(parentTransform, "Add Archetypes", properties.AddArchetypesToNormalNodes, SetAddArchetypes);
      archetypesTransform.SetAsLastSibling();
      manager.CreateListUIField(archetypesTransform, "Normal Node Archetypes", properties.NormalNodeArchetypes);
      manager.CreateSpaceUIField(parentTransform);

      var forcedTilesTransform = manager.CreateVerticalLayoutUIField(parentTransform);
      forcedTilesParentGameobject = forcedTilesTransform.gameObject;
      manager.CreateHeaderUIField(parentTransform, "Forced Tiles");
      manager.CreateBoolInputField(parentTransform, "Use Forced Tiles", properties.UseForcedTiles, SetUseForcedTiles);
      forcedTilesTransform.SetAsLastSibling();
      manager.CreateListUIField(forcedTilesTransform, "Forced Tile Sets", properties.ForcedTileSets);
      manager.CreateSpaceUIField(parentTransform);

      var branchLoopTransform = manager.CreateVerticalLayoutUIField(parentTransform);
      branchLoopBoostParentGameobject = branchLoopTransform.gameObject;
      manager.CreateHeaderUIField(parentTransform, "Branch Loop Boost");
      manager.CreateBoolInputField(parentTransform, "Use Branch Loop Boost", properties.UseBranchLoopBoost, SetUseBranchLoopBoost);
      branchLoopTransform.SetAsLastSibling();
      manager.CreateIntInputField(branchLoopTransform, "Tile Search Count", new IntParameter(properties.BranchLoopBoostTileSearch, 1, 100, 1), SetTileBoostSearch);
      manager.CreateFloatInputField(branchLoopTransform, "Tile Boost Search", new FloatParameter(properties.BranchLoopBoostTileScale, 0f, 2f, 0f), SetTileBoostScale);
      manager.CreateSpaceUIField(parentTransform);

      var maxShadowsTransform = manager.CreateVerticalLayoutUIField(parentTransform);
      maxShadowsParentGameobject = maxShadowsTransform.gameObject;
      manager.CreateHeaderUIField(parentTransform, "Max Shadows Request");
      manager.CreateBoolInputField(parentTransform, "Use Max Shadows Request", properties.UseMaxShadowsRequestUpdate, SetUseMaxShadows);
      maxShadowsTransform.SetAsLastSibling();
      manager.CreateIntInputField(maxShadowsTransform, "Shadows Request Amount", new IntParameter(properties.MaxShadowsRequestAmount, 4, 20, 4), SetMaxShadowsAmount);
      manager.CreateSpaceUIField(parentTransform);

      // miss
      manager.CreateHeaderUIField(parentTransform, "Miscellaneous");
      manager.CreateBoolInputField(parentTransform, "Use Doorway Sisters", properties.UseDoorwaySisters, SetUseDoorwaySisters);
      manager.CreateBoolInputField(parentTransform, "Use Random Guaranteed Scrap", properties.UseRandomGuaranteedScrapSpawn, SetUseRandomGuaranteedScrap);
      manager.CreateSpaceUIField(parentTransform);

      dungeonBoundsHelperGameObject.SetActive(selectedExtenderer.Properties.UseDungeonBounds);
      UpdateDungeonBoundsHelper();
    }

    public void ClearPanel(){
      manager.ClearTransformChildren(selectedListGameObject.transform);
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

    public void SetUseForcedTiles(bool state){
      selectedExtenderer.Properties.UseForcedTiles = state;
      forcedTilesParentGameobject.SetActive(state);
    }

    public void SetUseBranchLoopBoost(bool state){
      selectedExtenderer.Properties.UseBranchLoopBoost = state;
      branchLoopBoostParentGameobject.SetActive(state);
    }

    public void SetTileBoostSearch(int value){
      selectedExtenderer.Properties.BranchLoopBoostTileSearch = value;
    }

    public void SetTileBoostScale(float value){
      selectedExtenderer.Properties.BranchLoopBoostTileScale = value;
    }

    public void SetUseMaxShadows(bool state){
      selectedExtenderer.Properties.UseMaxShadowsRequestUpdate = state;
      maxShadowsParentGameobject.SetActive(state);
    }

    public void SetMaxShadowsAmount(int value){
      selectedExtenderer.Properties.MaxShadowsRequestAmount = value;
    }

    public void SetUseDoorwaySisters(bool state){
      selectedExtenderer.Properties.UseDoorwaySisters = state;
    }

    public void SetUseRandomGuaranteedScrap(bool state){
      selectedExtenderer.Properties.UseRandomGuaranteedScrapSpawn = state;
    }

    public void RestoreOriginalState(){
      selectedExtenderer.Properties.CopyFrom(selectedAssetCache.originalProperties);
    }

  }
}
