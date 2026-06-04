using SiraUtil.Affinity;
using SiraUtil.Attributes;

namespace ScoreSaber.Features.Replays.HarmonyPatches {
    [Bind]
    internal class FPFCPatch : IAffinity {

        private readonly bool _needsPatching;
        private readonly ReplayState _replayState;

        public FPFCPatch(IVRPlatformHelper vrPlatformHelper, ReplayState replayState) {
            _replayState = replayState;
            _needsPatching = vrPlatformHelper is OculusVRHelper || vrPlatformHelper is UnityXRHelper;
        }

        [AffinityPatch(typeof(OculusVRHelper), nameof(OculusVRHelper.hasInputFocus), AffinityMethodType.Getter)]
        protected void ForceInputFocusOculusVR(ref bool __result) {
            if (_needsPatching && _replayState.IsPlaybackEnabled)
                __result = true;
        }

        [AffinityPatch(typeof(UnityXRHelper), nameof(UnityXRHelper.hasInputFocus), AffinityMethodType.Getter)]
        protected void ForceInputFocusUnityXR(ref bool __result) {
            if (_needsPatching && _replayState.IsPlaybackEnabled)
                __result = true;
        }
    }
}
