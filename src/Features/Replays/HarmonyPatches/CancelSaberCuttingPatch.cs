using SiraUtil.Affinity;

namespace ScoreSaber.Features.Replays.HarmonyPatches {
    internal class CancelSaberCuttingPatch : IAffinity {

        private readonly SaberManager _saberManager;

        public CancelSaberCuttingPatch(SaberManager saberManager) {

            _saberManager = saberManager;
        }

        [AffinityPrefix, AffinityPatch(typeof(NoteCutter), nameof(NoteCutter.Cut))]
        private bool CancelCut(Saber saber) {

            return !(saber == _saberManager.leftSaber || saber == _saberManager.rightSaber);
        }
    }
}
