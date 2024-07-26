using DunGen.Graph;
using DunGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Collections {

  [System.Serializable]
  public class DunGenExtenderProperties {

    [Header("Main Path")]
    [Tooltip("The number of main paths.\n\n1 means no additional main paths\n3 means two additional main paths\netc.")]
    [Range(1, 9)]
    public int MainPathCount = 1;
    [Tooltip("The Tile Prefab where the additional main paths will start from. Cannot be null if MainPathCount is more than 1.\n\nHighly advice for this Tile Prefab to have multiple doorways.")]
    public GameObject MainRoomTilePrefab;

    [Header("Dungeon Bounds")]
    [Tooltip("If enabled, restricts the dungeon's generation to the bounds described below.\n\nThis will help in condensing the dungeon, but it will increase the chance of dungeon generation failure (potentially guarantees failure if the bounds is too small).")]
    public bool UseDungeonBounds = false;
    [Tooltip("The base size of the bounds.")]
    public Vector3 DungeonSizeBase = new Vector3(120f, 40f, 80f);
    [Tooltip("The factor that's multiplied with the base size AND the dungeon's size. The resulting value is added to the base size of the bounds.\n\n0 means that the bound size is not influenced by the dungeon's size and is therefore a constant.")]
    public Vector3 DungeonSizeFactor = new Vector3(1f, 0f, 1f);
    [Tooltip("The base positional offset of the bounds.")]
    public Vector3 DungeonPositionOffset;
    [Tooltip("The pivot of the bounds.")]
    public Vector3 DungeonPositionPivot = new Vector3(0.5f, 0f, 0.5f);

    [Header("Archetypes on Normal Nodes")]
    [Tooltip("If enabled, adds archetypes to the normal nodes in the DungeonFlow.\n\nBy default, nodes cannot have branching paths since they don't have archetype references. This allows nodes to have branching paths.")]
    public bool AddArchetypesToNormalNodes = true;
    public List<NodeArchetype> NormalNodeArchetypes;
    internal Dictionary<string, NodeArchetype> _normalNodeArchetypesDictioanry;
    internal NodeArchetype _defaultNodeArchetype;

    [Header("Doorway Sisters")]
    [Tooltip("If enabled, the DoorwaySisters component will become active.\n\nThe component prevents an intersecting doorway from generating if it's 'sister' doorway already generated and both doorways would lead to the same neighboring tile.\n\nThis is designed for the scenario where, two neighboring doorways would lead to the same tile, one doorway is a locked door and the other is an open doorway. This would defeat the purpose of the locked door, and such as, this feature exists if needed.\n\nThis feature slows down dungeon generation slightly when enabled.")]
    public bool UseDoorwaySisters = false;

    [Header("Line Randomizer")]
    [Tooltip("If enabled, every archetype in LineRandomizerArchetypes will have the last LineRandomizerTakeCount tilesets replaced by a randomly selected set of tilesets from LineRandomizerTileSets. This applies for both archetype's TileSets and BranchCapTileSets.\n\nThis is designed for the scenario where dungeon generation takes a long time due to the combination of too many tiles and/or doorways in those tiles. This can reduce dungeon generation time while keeping some of the randomness of dungeon generation.\n\nAs stated previously, this WILL replace the last LineRandomizerTakeCount tilesets in the archetype's TileSets and BranchCapTileSets. As such you must guarantee that those elements can be replaced.")]
    public bool UseLineRandomizer = false;
    [Tooltip("The archetypes whose tilesets will be replaced.\n\nThese archetypes should ideally used in the Lines section of DungeonFlow, but it's a free country.")]
    public List<DungeonArchetype> LineRandomizerArchetypes;
    [Tooltip("The tilesets that will be used for replacement.")]
    public List<TileSet> LineRandomizerTileSets;
    [Tooltip("The amount of tilesets that will be replaced from the archetypes, starting from the last element to the first element.\n\nAs stated previously, this WILL replace the tilesets in the archetype's TileSets and BranchCapTileSets. As such you must guarantee that those elements can be replaced.")]
    public int LineRandomizerTakeCount = 3;

    [Header("Max Shadows Request")]
    [Tooltip("If enabled, updates the MaxShadowsRequest to MaxShadowsRequestAmount when your dungeon loads.\n\nThis is designed for the scenario where your dungeon, for whatever reason, has too many lights nearby and causes the annoying 'Max shadow requests count reached' warning to spam the logs.")]
    public bool UseMaxShadowsRequestUpdate = false;
    [Tooltip("The amount of MaxShadowsRequest.\n\n4 is the game's default value. I find 8 to be more than acceptable.")]
    public int MaxShadowsRequestAmount = 8;

    internal void SetupProperties(DungeonGenerator generator){
      _normalNodeArchetypesDictioanry = new Dictionary<string, NodeArchetype>();
      _defaultNodeArchetype = null;

      foreach(var n in NormalNodeArchetypes) {
        if (_normalNodeArchetypesDictioanry.ContainsKey(n.label)) {
          Plugin.logger.LogError($"Label {n.label} already exists. Ignoring latest entry.");
          continue;
        }
        _normalNodeArchetypesDictioanry.Add(n.label, n);

        if (string.IsNullOrWhiteSpace(n.label)) {
          _defaultNodeArchetype = n;
        }
      }
    }

    internal DungeonArchetype GetRandomArchetype(string label, RandomStream randomStream) {
      NodeArchetype node;
      if (!_normalNodeArchetypesDictioanry.TryGetValue(label, out node)) {
        node = _defaultNodeArchetype;
      }

      if (node != null) {
        var archetypes = node.archetypes;
        var count = archetypes.Count;
        if (count == 0) return null;

        var index = randomStream.Next(0, count);
        return archetypes[index];
      }

      return null;
    }

    internal Bounds GetDungeonBounds(float dungeonScale) {
      var size = DungeonSizeBase + Vector3.Scale(DungeonSizeBase * (dungeonScale - 1), DungeonSizeFactor);
      var offset = DungeonPositionOffset + Vector3.Scale(size, DungeonPositionPivot);
      return new Bounds(offset, size);
    }

    internal DunGenExtenderProperties Copy() {
      var copy = new DunGenExtenderProperties();

      copy.MainPathCount = MainPathCount;
      copy.MainRoomTilePrefab = MainRoomTilePrefab;

      copy.UseDungeonBounds = UseDungeonBounds;
      copy.DungeonSizeBase = DungeonSizeBase;
      copy.DungeonSizeFactor = DungeonSizeFactor;
      copy.DungeonPositionOffset = DungeonPositionOffset;
      copy.DungeonPositionPivot = DungeonPositionPivot;

      copy.AddArchetypesToNormalNodes = AddArchetypesToNormalNodes;
      copy.NormalNodeArchetypes = NormalNodeArchetypes;

      copy.UseDoorwaySisters = UseDoorwaySisters;

      copy.UseLineRandomizer = UseLineRandomizer;
      copy.LineRandomizerTileSets = LineRandomizerTileSets; 
      copy.LineRandomizerArchetypes = LineRandomizerArchetypes;
      copy.LineRandomizerTakeCount = LineRandomizerTakeCount;

      copy.UseMaxShadowsRequestUpdate = UseMaxShadowsRequestUpdate;
      copy.MaxShadowsRequestAmount = MaxShadowsRequestAmount;

      return copy;
    }

  }
}
