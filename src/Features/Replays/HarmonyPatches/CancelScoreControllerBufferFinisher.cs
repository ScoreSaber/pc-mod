using HarmonyLib;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ScoreSaber.Features.Replays.HarmonyPatches {
    internal class CancelScoreControllerBufferFinisher : IAffinity {
        private static readonly FieldInfo _multScore = typeof(ScoreController).GetField("_multipliedScore", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _immediateScore = typeof(ScoreController).GetField("_immediateMaxPossibleMultipliedScore", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly SiraLog _siraLog;

        public CancelScoreControllerBufferFinisher(SiraLog siraLog) {

            _siraLog = siraLog;
        }

        [AffinityTranspiler]
        [AffinityPatch(typeof(ScoreController), nameof(ScoreController.LateUpdate))]
        protected IEnumerable<CodeInstruction> RemoveScoreUpdate(IEnumerable<CodeInstruction> instructions) {

            var codes = instructions.ToList();

            int? startIndex = null;
            int? endIndex = null;
            int count = 0;

#pragma warning disable CS0252 // Possible unintended reference comparison; left hand side needs cast
            for (int i = 0; i < codes.Count; i++) {

                if (!startIndex.HasValue) {

                    if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand == _multScore) {

                        startIndex = i - 1;
                        count = 2;
                    }
                } else if (!endIndex.HasValue) {

                    count++;
                    if (codes[i].opcode == OpCodes.Stfld && codes[i].operand == _immediateScore) {

                        endIndex = i;
                    }
                } else {

                    break;
                }
            }

            if (startIndex.HasValue && endIndex.HasValue) {

                codes.RemoveRange(startIndex.Value, count);
            } else {

                _siraLog.Error("Unable to cancel score controller buffer setters! Could not find IL group.");
            }

            return codes;
        }
    }
}
