using DunGen;
using DunGen.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Generation {
  internal partial class DunGenPlusGenerator {

		public static MainPathExtender currentMainPathExtender;

		public static void SetCurrentMainPathExtender(int mainPathIndex){
			currentMainPathExtender = Properties.MainPathProperties.GetMainPathDetails(mainPathIndex);
		}

    public static GraphLine GetLineAtDepth(DungeonFlow flow, float depth) {
      if (!DunGenPlusGenerator.Active) {
				//Plugin.logger.LogInfo("Default");
				return flow.GetLineAtDepth(depth);
			}

			//Plugin.logger.LogInfo(currentMainPathExtender == null ? "NULL" : "ITEM");
			var lines = MainPathExtender.GetLines(currentMainPathExtender, flow);
			return GetLineAtDepthHelper(lines, depth);
    }

    public static GraphLine GetLineAtDepthHelper(List<GraphLine> graphLines, float normalizedDepth) {
			normalizedDepth = Mathf.Clamp(normalizedDepth, 0f, 1f);
			if (normalizedDepth == 0f) return graphLines[0];
			if (normalizedDepth == 1f) return graphLines[graphLines.Count - 1];
			foreach (GraphLine graphLine in graphLines){
				if (normalizedDepth >= graphLine.Position && normalizedDepth < graphLine.Position + graphLine.Length) {
					return graphLine;
				}
			}
			Debug.LogError("GetLineAtDepth was unable to find a line at depth " + normalizedDepth.ToString() + ". This shouldn't happen.");
			return null;
		}

		public static List<GraphNode> GetNodes(DungeonFlow flow){
			if (!DunGenPlusGenerator.Active) {
				//Plugin.logger.LogInfo("Default");
				return flow.Nodes;
			}

			//Plugin.logger.LogInfo(currentMainPathExtender == null ? "NULL" : "ITEM");
			return MainPathExtender.GetNodes(currentMainPathExtender, flow);
		}

		public static BranchMode GetBranchMode(DungeonFlow flow) {
			if (!DunGenPlusGenerator.Active) {
				//Plugin.logger.LogInfo("Default M");
				return flow.BranchMode;
			}

			//Plugin.logger.LogInfo(currentMainPathExtender == null ? "NULL" : "ITEM M");
			return MainPathExtender.GetBranchMode(currentMainPathExtender, flow);
		}

		public static IntRange GetBranchCount(DungeonFlow flow) {
			if (!DunGenPlusGenerator.Active) {
				//Plugin.logger.LogInfo("Default C");
				return flow.BranchCount;
			}

			//Plugin.logger.LogInfo(currentMainPathExtender == null ? "NULL" : "ITEM C");
			return MainPathExtender.GetBranchCount(currentMainPathExtender, flow);
		}

		public static IntRange GetLength(DungeonFlow flow) {
			if (!DunGenPlusGenerator.Active) {
				Plugin.logger.LogInfo("Default");
				return flow.Length;
			}

			Plugin.logger.LogInfo(currentMainPathExtender == null ? "NULL" : "ITEM");
			return MainPathExtender.GetLength(currentMainPathExtender, flow);
		}

  }
}
