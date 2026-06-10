using IPA.Utilities;
using UnityEngine;
#if !BEAT_SABER_1_29_0
using UnityEngine.SpatialTracking;
#endif

namespace ScoreSaber.Core.Compat {
    // 1.29 doesn't have these camera bits, so there is nothing to clone there
    internal static class ReplayCameraCompat {
        internal static void CopyTrackedPoseDriver(MainCamera mainCamera, Camera spectatorCamera) {
#if !BEAT_SABER_1_29_0
            mainCamera.gameObject.GetComponent<TrackedPoseDriver>().CopyComponent<TrackedPoseDriver>(spectatorCamera.gameObject);
#endif
        }

        internal static void RebuildDepthTextureController(MainCamera mainCamera, Camera spectatorCamera) {
#if !BEAT_SABER_1_29_0
            // recreate this since Instantiate leaves it without its Zenject objects
            Component.Destroy(spectatorCamera.gameObject.GetComponent<DepthTextureController>());
            mainCamera.gameObject.GetComponent<DepthTextureController>().CopyComponent<DepthTextureController>(spectatorCamera.gameObject);
#endif
        }
    }
}
