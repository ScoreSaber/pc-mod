using IPA.Utilities;
using ScoreSaber.Core.Compat;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Replays.Format;
using SiraUtil.Tools.FPFC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Replays.Playback {
    internal class PosePlayer : TimeSynchronizer, IInitializable, ITickable, IScroller, IDisposable {
        private int _nextIndex = 0;
        private readonly MainCamera _mainCamera;
        private readonly SaberManager _saberManager;
        private readonly List<VRPoseGroup> _poses;
        private readonly IFPFCSettings _fpfcSettings;
        private readonly RoomSettings _roomSettings;
        private readonly SettingsService _settings;
        private readonly IReturnToMenuController _returnToMenuController;
        public event Action<VRPoseGroup> DidUpdatePose;
        private PlayerTransforms _playerTransforms;
        private Camera _spectatorCamera;
        private Camera _desktopCamera;
        private bool _saberEnabled = true;
        private Vector3 _spectatorOffset;

        private bool initialFPFCState;

        public PosePlayer(ReplayFile file, MainCamera mainCamera, SaberManager saberManager, IReturnToMenuController returnToMenuController, IFPFCSettings fpfcSettings, PlayerTransforms playerTransforms, RoomSettings roomSettings, SettingsService settings) {

            _fpfcSettings = fpfcSettings;
            initialFPFCState = fpfcSettings.Enabled;
            _fpfcSettings.Enabled = false;

            _mainCamera = mainCamera;
            _saberManager = saberManager;
            _poses = file.poseKeyframes;
            _returnToMenuController = returnToMenuController;
            _spectatorOffset = new Vector3(0f, 0f, -2f);
            _roomSettings = roomSettings;
            _settings = settings;
            _playerTransforms = playerTransforms;
        }

        public void Initialize() {

            SetupCameras();
            _saberManager.leftSaber.transform.GetComponentInParent<VRController>().enabled = false;
            _saberManager.rightSaber.transform.GetComponentInParent<VRController>().enabled = false;
            _fpfcSettings.AddChangedListener(fpfcSettings_Changed);
        }

        private void fpfcSettings_Changed(IFPFCSettings fpfcSettings) {

            if (fpfcSettings.Enabled) {
                _desktopCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        private void SetupCameras() {

            _mainCamera.enabled = false;
            _mainCamera.gameObject.SetActive(false);
            _desktopCamera = Resources.FindObjectsOfTypeAll<Camera>().First(x => (x.name == "RecorderCamera"));

            _desktopCamera.fieldOfView = _settings.Current.replayCameraFOV;
            _desktopCamera.transform.position = new Vector3(_desktopCamera.transform.position.x, _desktopCamera.transform.position.y, _desktopCamera.transform.position.z);
            _desktopCamera.gameObject.SetActive(true);
            _desktopCamera.tag = "MainCamera";
            _desktopCamera.depth = 1;

            _mainCamera.SetField("_camera", _desktopCamera);

            // camera2 finds these cameras by object path, so keep the names and hierarchy stable
            GameObject spectatorObject = new GameObject("SpectatorParent");
            _spectatorCamera = UnityEngine.Object.Instantiate(_desktopCamera);
            spectatorObject.transform.position = _roomSettings.Center + _spectatorOffset;
            Quaternion rotation = new Quaternion {
                eulerAngles = new Vector3(0.0f, _roomSettings.Rotation, 0.0f)
            };
            spectatorObject.transform.rotation = rotation;
            _spectatorCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            ReplayCameraCompat.CopyTrackedPoseDriver(_mainCamera, _spectatorCamera);
            ReplayCameraCompat.RebuildDepthTextureController(_mainCamera, _spectatorCamera);

            _spectatorCamera.gameObject.SetActive(true);
            _spectatorCamera.depth = 0;
            _spectatorCamera.transform.SetParent(spectatorObject.transform);

            if (_settings.Current.enableReplayFrameRenderer) {
                var ss = Resources.FindObjectsOfTypeAll<ScreenshotRecorder>().Last();
                ss.SetField("_directory", _settings.Current.replayFramePath);
                ss.enabled = true;
                _desktopCamera.depth = 1;
                var gc = Resources.FindObjectsOfTypeAll<DisableGCWhileEnabled>().Last();
                gc.enabled = false;
            }
        }

        public void Tick() {

            if (ReachedEnd()) {
                _returnToMenuController.ReturnToMenu();
                return;
            }

            bool foundPoseThisFrame = false;
            while (audioTimeSyncController.songTime >= _poses[_nextIndex].Time) {
                foundPoseThisFrame = true;
                VRPoseGroup activePose = _poses[_nextIndex++];

                if (ReachedEnd())
                    return;

                VRPoseGroup nextPose = _poses[_nextIndex];
                UpdatePoses(activePose, nextPose);
            }
            if (!foundPoseThisFrame && _nextIndex > 0 && !ReachedEnd()) {
                VRPoseGroup previousGroup = _poses[_nextIndex - 1];
                UpdatePoses(previousGroup, _poses[_nextIndex]);
            }
        }

        private void UpdatePoses(VRPoseGroup activePose, VRPoseGroup nextPose) {

            if (Input.GetKeyDown(KeyCode.H))
                _saberEnabled = !_saberEnabled;

            float lerpTime = (audioTimeSyncController.songTime - activePose.Time) / Mathf.Max(0.000001f, nextPose.Time - activePose.Time);


            var originParentTransform = _playerTransforms._originParentTransform;

            if (_saberEnabled) {

                _saberManager.leftSaber.OverridePositionAndRotation(
                    originParentTransform.TransformPoint(Vector3.Lerp(activePose.Left.Position.Convert(), nextPose.Left.Position.Convert(), lerpTime)),
                    originParentTransform.rotation * Quaternion.Lerp(activePose.Left.Rotation.Convert(), nextPose.Left.Rotation.Convert(), lerpTime)
                );

                _saberManager.rightSaber.OverridePositionAndRotation(
                    originParentTransform.TransformPoint(Vector3.Lerp(activePose.Right.Position.Convert(), nextPose.Right.Position.Convert(), lerpTime)),
                    originParentTransform.rotation * Quaternion.Lerp(activePose.Right.Rotation.Convert(), nextPose.Right.Rotation.Convert(), lerpTime)
                );
            } else {
                _saberManager.leftSaber.OverridePositionAndRotation(Vector3.zero, new Quaternion(0, 0, 0, 0));
                _saberManager.rightSaber.OverridePositionAndRotation(Vector3.zero, new Quaternion(0, 0, 0, 0));
            }


            var pos = originParentTransform.TransformPoint(Vector3.Lerp(activePose.Head.Position.Convert(), nextPose.Head.Position.Convert(), lerpTime));
            var rot = originParentTransform.rotation * Quaternion.Lerp(activePose.Head.Rotation.Convert(), nextPose.Head.Rotation.Convert(), lerpTime);

            _playerTransforms._headTransform.SetPositionAndRotation(pos, rot);

            var eulerAngles = rot.eulerAngles;
            Vector3 headRotationOffset = new Vector3(_settings.Current.replayCameraXRotation, _settings.Current.replayCameraYRotation, _settings.Current.replayCameraZRotation);
            eulerAngles += headRotationOffset;
            rot.eulerAngles = eulerAngles;

            float t2 = _settings.Current.replayCameraSmoothing ? Time.deltaTime * 6f : 1.0f;
            pos.x += _settings.Current.replayCameraXOffset;
            pos.y += _settings.Current.replayCameraYOffset;
            pos.z += _settings.Current.replayCameraZOffset;

            if (!_fpfcSettings.Enabled) {
                _desktopCamera.transform.SetPositionAndRotation(Vector3.Lerp(_desktopCamera.transform.position, pos, t2), Quaternion.Lerp(_desktopCamera.transform.rotation, rot, t2));
            }

            DidUpdatePose?.Invoke(activePose);
        }

        private bool ReachedEnd() => _nextIndex >= _poses.Count;

        public void TimeUpdate(float newTime) {

            _nextIndex = ReplayTimeSearch.CountAtOrBefore(_poses, newTime, pose => pose.Time);
            if (!ReachedEnd()) {
                Tick();
            }
        }

        public void SetSpectatorOffset(Vector3 value) {

            _spectatorCamera.transform.parent.position = _roomSettings.Center + value;

            _spectatorOffset = value;
        }

        public void Dispose() {
            _fpfcSettings.RemoveChangedListener(fpfcSettings_Changed);
            _fpfcSettings.Enabled = initialFPFCState;
        }
    }
}
