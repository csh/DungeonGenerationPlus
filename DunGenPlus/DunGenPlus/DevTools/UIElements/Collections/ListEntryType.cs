using DunGen;
using DunGen.Graph;
using DunGenPlus.Collections;
using DunGenPlus.DevTools.Panels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.DevTools.UIElements.Collections {
  internal abstract class ListEntryType {
    public abstract object CreateEmptyObject();
    public abstract void CreateEntry(IList list, int index, Transform parentTransform, float layoutOffset);
  }

  internal class ListEntryDungeonArchetype : ListEntryType {
    public override object CreateEmptyObject() => null;

    public override void CreateEntry(IList list, int index, Transform parentTransform, float layoutOffset) {
      var entry = (DungeonArchetype)list[index];
      var baseValue = DevDebugManager.Instance.selectedAssetCache.archetypes.dictionary[entry];
      DevDebugManager.Instance.CreateArchetypeOptionsUIField(parentTransform, new TitleParameter("Archetype", layoutOffset), baseValue, (t) => list[index] = t);
    }
  }

  internal class ListEntryTileSet : ListEntryType {
    public override object CreateEmptyObject() => null;

    public override void CreateEntry(IList list, int index, Transform parentTransform, float layoutOffset) {
      var entry = (TileSet)list[index];
      var baseValue = DevDebugManager.Instance.selectedAssetCache.tileSets.dictionary[entry];
      DevDebugManager.Instance.CreateTileSetsOptionsUIField(parentTransform, new TitleParameter("Tile Set", layoutOffset), baseValue, (t) => list[index] = t);
    }
  }

  internal class ListEntryMainPathExtender : ListEntryType {
    public override object CreateEmptyObject() => null;

    public override void CreateEntry(IList list, int index, Transform parentTransform, float layoutOffset) {
      var entry = (MainPathExtender)list[index];
      var baseValue = DevDebugManager.Instance.selectedAssetCache.mainPathExtenders.dictionary[entry];
      DevDebugManager.Instance.CreateMainPathExtenderUIField(parentTransform, new TitleParameter("Main Path Extender", layoutOffset), baseValue, (t) => list[index] = t);
    }
  }

  internal class ListEntryNodeArchetype : ListEntryType {
    public override object CreateEmptyObject() => new NodeArchetype();

    public override void CreateEntry(IList list, int index, Transform parentTransform, float layoutOffset) {
      var entry = (NodeArchetype)list[index];
      DevDebugManager.Instance.CreateStringInputField(parentTransform, new TitleParameter("Label", NodeArchetype.LabelTooltip, layoutOffset), entry.Label, (t) => entry.Label = t);
      DevDebugManager.Instance.CreateListUIField(parentTransform, new TitleParameter("Archetypes", NodeArchetype.ArchetypesTooltip, layoutOffset), entry.Archetypes);
    }
  }

  internal class ListEntryAdditionalTileSetList : ListEntryType {
    public override object CreateEmptyObject() {
      var forcedTileset = new AdditionalTileSetList(); 
      forcedTileset.DepthWeightScale = null;
      return forcedTileset;
    }

    public override void CreateEntry(IList list, int index, Transform parentTransform, float layoutOffset) {
      var entry = (AdditionalTileSetList)list[index];
      DevDebugManager.Instance.CreateFloatInputField(parentTransform, new TitleParameter("Main Path Weight", AdditionalTileSetList.MainPathWeightTooltip, layoutOffset), entry.MainPathWeight, (t) => entry.MainPathWeight = t);
      DevDebugManager.Instance.CreateFloatInputField(parentTransform, new TitleParameter("Branch Path Weight", AdditionalTileSetList.BranchPathWeightTooltip, layoutOffset), entry.BranchPathWeight, (t) => entry.BranchPathWeight = t);

      // depth is weird cause we have to account for every entry's unique depth curve, even if they don't have one
      DevDebugManager.Instance.CreateAnimationCurveOptionsUIField(parentTransform, new TitleParameter("Depth Weight Scale", AdditionalTileSetList.DepthWeightScaleTooltip, layoutOffset), entry.DepthWeightScale, (t) => entry.DepthWeightScale = t);
      DevDebugManager.Instance.CreateListUIField(parentTransform, new TitleParameter("Tile Sets", AdditionalTileSetList.TileSetsTooltip, layoutOffset), entry.TileSets);
    }
  }

  internal class ListEntryTileInjectionRule : ListEntryType {

    public override object CreateEmptyObject() => new TileInjectionRule();

    public override void CreateEntry(IList list, int index, Transform parentTransform, float layoutOffset){
      var entry = (TileInjectionRule)list[index];
      var baseValue = DevDebugManager.Instance.selectedAssetCache.tileSets.dictionary[entry.TileSet];
      DevDebugManager.Instance.CreateTileSetsOptionsUIField(parentTransform, "Tile Set", baseValue, (t) => entry.TileSet = t);

      DevDebugManager.Instance.CreateFloatRangeInputField(parentTransform, "Norm. Path Depth", entry.NormalizedPathDepth, (t) => entry.NormalizedPathDepth = t);
      DevDebugManager.Instance.CreateFloatRangeInputField(parentTransform, "Norm. Branch Depth", entry.NormalizedBranchDepth, (t) => entry.NormalizedBranchDepth = t);

      DevDebugManager.Instance.CreateBoolInputField(parentTransform, "Appear On Main Path", entry.CanAppearOnMainPath, (t) => entry.CanAppearOnMainPath = t);
      DevDebugManager.Instance.CreateBoolInputField(parentTransform, "Appear On Branch Path", entry.CanAppearOnBranchPath, (t) => entry.CanAppearOnBranchPath = t);
      DevDebugManager.Instance.CreateBoolInputField(parentTransform, "Is Required", entry.IsRequired, (t) => entry.IsRequired = t);
    }
  }

  internal class ListEntryGraphNode : ListEntryType {

    public override object CreateEmptyObject() => new GraphNode(DevDebugManager.Instance.selectedDungeonFlow);

    public override void CreateEntry(IList list, int index, Transform parentTransform, float layoutOffset){
      var entry = (GraphNode)list[index];
      DevDebugManager.Instance.CreateListUIField(parentTransform, "Tile Sets", entry.TileSets);
      DevDebugManager.Instance.CreateEnumOptionsUIField<NodeType>(parentTransform, "Node Type", (int)entry.NodeType, (t) => entry.NodeType = t);
      DevDebugManager.Instance.CreateFloatInputField(parentTransform, "Position", new FloatParameter(entry.Position, 0f, 1f), (t) => entry.Position = t);
      DevDebugManager.Instance.CreateStringInputField(parentTransform, "Label", entry.Label, (t) => entry.Label = t);
    }
  }

  internal class ListEntryGraphLine : ListEntryType {

    public override object CreateEmptyObject() => new GraphLine(DevDebugManager.Instance.selectedDungeonFlow);

    public override void CreateEntry(IList list, int index, Transform parentTransform, float layoutOffset){
      var entry = (GraphLine)list[index];
      DevDebugManager.Instance.CreateListUIField(parentTransform, "Dungeon Archetypes", entry.DungeonArchetypes);
      DevDebugManager.Instance.CreateFloatInputField(parentTransform, "Position", new FloatParameter(entry.Position, 0f, 1f), (t) => entry.Position = t);
      DevDebugManager.Instance.CreateFloatInputField(parentTransform, "Length", new FloatParameter(entry.Length, 0f, 1f), (t) => entry.Length = t);
    }
  }

}
