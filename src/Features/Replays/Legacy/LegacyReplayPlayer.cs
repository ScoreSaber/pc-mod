using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;
using SiraUtil.Tools.FPFC;
using UnityEngine;
using IPA.Utilities;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Core.Gameplay;
using UnityEngine.SpatialTracking;

namespace ScoreSaber.Features.Replays.Legacy {

    internal class LegacyReplayPlayer : IInitializable, ITickable, IDisposable {

        private ScoreController _scoreController;
        private readonly ScoreUIController _scoreUIController;
        private readonly RelativeScoreAndImmediateRankCounter _relativeScoreAndImmediateRankCounter;
        private readonly AudioTimeSyncController _audioTimeSyncController;
        private readonly MainCamera _mainCamera;
        private readonly SaberManager _saberManager;
        private readonly SettingsManager _settingsManager;
        private readonly SettingsService _settings;
        private PlayerTransforms _playerTransforms;
        private readonly IFPFCSettings _fpfcSettings;
        private ComboController _comboController;

        private Camera _spectatorCamera;
        private Camera _desktopCamera;

        public int cutOrMissedNotes;
        private int _lastKeyframeIndex;
        private int _multiplier;
        private int _multiplierIncreaseProgress;
        private int _multiplierIncreaseMaxProgress;
        private int _playbackPreviousCombo;
        private int _playbackPreviousScore;
        private bool _initialFPFCState;
        private List<Z.Keyframe> _keyframes;

        internal LegacyReplayPlayer(List<Z.Keyframe> keyframes, ScoreController scoreController,
            RelativeScoreAndImmediateRankCounter relativeScoreAndImmediateRankCounter, AudioTimeSyncController audioTimeSyncController,
            MainCamera mainCamera, SaberManager saberManager, PlayerTransforms playerTransforms, IFPFCSettings fpfcSettings, ComboController comboController, SettingsManager settingsManager, SettingsService settings) {

            _fpfcSettings = fpfcSettings;
            _comboController = comboController;
            _initialFPFCState = fpfcSettings.Enabled;
            _fpfcSettings.Enabled = false;

            _keyframes = keyframes;
            _scoreController = scoreController;

            _relativeScoreAndImmediateRankCounter = relativeScoreAndImmediateRankCounter;
            _audioTimeSyncController = audioTimeSyncController;
            _mainCamera = mainCamera;
            _saberManager = saberManager;
            _playerTransforms = playerTransforms;
            _settingsManager = settingsManager;
            _settings = settings;
            _scoreUIController = Resources.FindObjectsOfTypeAll<ScoreUIController>().FirstOrDefault();
        }

        public void Initialize() {
            SetupCameras();
            _fpfcSettings.Changed += fpfcSettings_Changed;
            ScoreUIController.InitData data = new ScoreUIController.InitData(scoreDisplayType: ScoreUIController.ScoreDisplayType.MultipliedScore);
            _scoreUIController.SetField("_initData", data);
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


            GameObject spectatorObject = new GameObject("SpectatorParent");
            _spectatorCamera = UnityEngine.Object.Instantiate(_desktopCamera);
            spectatorObject.transform.position = new Vector3(_settingsManager.settings.room.center.x, _settingsManager.settings.room.center.y, _settingsManager.settings.room.center.z);
            Quaternion rotation = new Quaternion {
                eulerAngles = new Vector3(0.0f, _settingsManager.settings.room.rotation, 0.0f)
            };
            spectatorObject.transform.rotation = rotation;
            _spectatorCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            _mainCamera.gameObject.GetComponent<TrackedPoseDriver>().CopyComponent<TrackedPoseDriver>(_spectatorCamera.gameObject);
            _spectatorCamera.gameObject.SetActive(true);
            _spectatorCamera.depth = 0;
            _spectatorCamera.transform.SetParent(spectatorObject.transform);

            if (_settings.Current.enableReplayFrameRenderer) {
                var ss = Resources.FindObjectsOfTypeAll<ScreenshotRecorder>().Last();
                ss.SetField("_folder", _settings.Current.replayFramePath);
                ss.enabled = true;
                _desktopCamera.depth = 1;
                var gc = Resources.FindObjectsOfTypeAll<DisableGCWhileEnabled>().Last();
                gc.enabled = false;
            }
        }

        public void Tick() {

            float time = _audioTimeSyncController.songTime;
            int keyframeIndex = 0;

            while (keyframeIndex < (_keyframes.Count - 2) && _keyframes[keyframeIndex + 1]._time < time) {
                keyframeIndex++;
            }

            Z.Keyframe keyframe1 = _keyframes[keyframeIndex];
            Z.Keyframe keyframe2 = _keyframes[keyframeIndex + 1];

            if (keyframe1 == null || keyframe2 == null) { return; }
            float t = (time - keyframe1._time) / Mathf.Max(0.000001f, keyframe2._time - keyframe1._time);

            _saberManager.rightSaber.OverridePositionAndRotation(
                  Vector3.Lerp(keyframe1._pos1, keyframe2._pos1, t),
                  Quaternion.Lerp(keyframe1._rot1, keyframe2._rot1, t)
              );

            _saberManager.leftSaber.OverridePositionAndRotation(
                Vector3.Lerp(keyframe1._pos2, keyframe2._pos2, t),
              Quaternion.Lerp(keyframe1._rot2, keyframe2._rot2, t)
            );

            var pos = Vector3.Lerp(keyframe1._pos3, keyframe2._pos3, t);
            Quaternion rot = Quaternion.Lerp(keyframe1._rot3, keyframe2._rot3, t);
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

            if (_scoreController != null && cutOrMissedNotes >= 1) {
                UpdatePlaybackScore(keyframe1);
                _lastKeyframeIndex = keyframeIndex;
            }
        }

        private void UpdatePlaybackScore(Z.Keyframe keyframe) {

            bool comboChanged = false;
            bool multiplierChanged = false;

            if (_playbackPreviousCombo != keyframe.combo) {
                comboChanged = true;
                _comboController._combo = keyframe.combo;
            }

            if (_playbackPreviousScore != keyframe.score) {

                int maxPossibleRawScore = ScoreSaberScoreModel.OldMaxRawScoreForNumberOfNotes(cutOrMissedNotes);

                _relativeScoreAndImmediateRankCounter.UpdateRelativeScoreAndImmediateRank(keyframe.score, keyframe.score, maxPossibleRawScore, maxPossibleRawScore);
                _scoreUIController.UpdateScore(keyframe.score, keyframe.score);

            }

            PlaybackMultiplierCheck(keyframe, comboChanged, ref multiplierChanged);

            _playbackPreviousCombo = keyframe.combo;
            _playbackPreviousScore = keyframe.score;

            if (comboChanged) {
                FieldAccessor<ScoreController, Action<int, int>>.Get(_scoreController, "scoreDidChangeEvent").Invoke(keyframe.score,
                    ScoreModel.GetModifiedScoreForGameplayModifiersScoreMultiplier(keyframe.score, _scoreController._prevMultiplierFromModifiers));
            }

            if (multiplierChanged) {

                FieldAccessor<ScoreController, Action<int, float>>.Get(_scoreController, "multiplierDidChangeEvent").Invoke(_multiplier, _multiplierIncreaseProgress);
            }
        }

        private void PlaybackMultiplierCheck(Z.Keyframe keyframe, bool comboChanged, ref bool multiplierChanged) {

            if (keyframe.combo > _playbackPreviousCombo) {
                if (_multiplier < 8) {

                    var counter = _scoreController._scoreMultiplierCounter;

                    if (_multiplierIncreaseProgress < _multiplierIncreaseMaxProgress) {
                        _multiplierIncreaseProgress++;

                        counter._multiplierIncreaseProgress = _multiplierIncreaseProgress;
                        multiplierChanged = true;
                    }
                    if (_multiplierIncreaseProgress >= _multiplierIncreaseMaxProgress) {
                        _multiplier *= 2;
                        _multiplierIncreaseProgress = 0;
                        _multiplierIncreaseMaxProgress = _multiplier * 2;

                        counter._multiplier = _multiplier;
                        counter._multiplierIncreaseProgress = _multiplierIncreaseProgress;
                        counter._multiplierIncreaseMaxProgress = _multiplierIncreaseMaxProgress;

                        multiplierChanged = true;
                    }
                }
            } else if (keyframe.combo < _playbackPreviousCombo) {
                if (_multiplierIncreaseProgress > 0) {
                    _multiplierIncreaseProgress = 0;
                    multiplierChanged = true;
                }
                if (_multiplier > 1) {
                    _multiplier /= 2;
                    _multiplierIncreaseMaxProgress = _multiplier * 2;
                    multiplierChanged = true;
                }

                var counter = _scoreController._scoreMultiplierCounter;
                counter.ProcessMultiplierEvent(ScoreMultiplierCounter.MultiplierEventType.Negative);
                FieldAccessor<ScoreController, Action<int, float>>.Get(_scoreController, "multiplierDidChangeEvent").Invoke(_multiplier, _multiplierIncreaseProgress);
            }
        }

        public bool IsRealMiss() {

            int lastCombo = _keyframes[Math.Max(_lastKeyframeIndex - 10, 0)].combo;

            for (int i = Math.Max(_lastKeyframeIndex - 10, 0); i < Math.Min(_lastKeyframeIndex + 10, _keyframes.Count); i++) {
                if (_keyframes[i].combo < lastCombo) {
                    return true;
                }
                lastCombo = _keyframes[i].combo;
            }
            return false;
        }

        public void Dispose() {
            _fpfcSettings.Changed -= fpfcSettings_Changed;
            _fpfcSettings.Enabled = _initialFPFCState;
        }
    }
}
