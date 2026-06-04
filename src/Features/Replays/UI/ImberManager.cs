using BeatSaberMarkupLanguage;
using HMUI;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Replays.Format;
using ScoreSaber.Features.Replays.Playback;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace ScoreSaber.Features.Replays.UI {
    internal class ImberManager : IInitializable, IDisposable {
        private readonly IGamePause _gamePause;
        private readonly float _initialTimeScale;
        private readonly PosePlayer _posePlayer;
        private readonly ImberScrubber _imberScrubber;
        private readonly ImberSpecsReporter _imberSpecsReporter;
        private readonly MainImberPanelView _mainImberPanelView;
        private readonly SpectateAreaController _spectateAreaController;
        private readonly AudioTimeSyncController _audioTimeSyncController;
        private readonly ReplayTimeSyncController _replayTimeSyncController;
        private readonly ImberUIPositionController _imberUIPositionController;
        private readonly SettingsService _settings;

        private readonly IEnumerable<string> _positions;

        public ImberManager(ReplayFile file, IGamePause gamePause, ImberScrubber imberScrubber, ImberSpecsReporter imberSpecsReporter, MainImberPanelView mainImberPanelView, SpectateAreaController spectateAreaController,
                            AudioTimeSyncController audioTimeSyncController, ReplayTimeSyncController replayTimeSyncController, ImberUIPositionController imberUIPositionController, AudioTimeSyncController.InitData initData, PosePlayer posePlayer, SettingsService settings) {

            _gamePause = gamePause;
            _posePlayer = posePlayer;
            _imberScrubber = imberScrubber;
            _imberSpecsReporter = imberSpecsReporter;
            _mainImberPanelView = mainImberPanelView;
            _spectateAreaController = spectateAreaController;
            _audioTimeSyncController = audioTimeSyncController;
            _replayTimeSyncController = replayTimeSyncController;
            _imberUIPositionController = imberUIPositionController;
            _settings = settings;
            _positions = _settings.Current.spectatorPositions.Select(sp => sp.name);
            _mainImberPanelView.Setup(initData.timeScale, 90, _positions.First(), _positions);
            _imberScrubber.Setup(file.metadata.FailTime, file.metadata.Modifiers.Contains("NF"));
            _initialTimeScale = file.noteKeyframes.FirstOrDefault().TimeSyncTimescale;
        }

        public void Initialize() {

            //MainImberPanelView_DidChangeVisiblity(true); // Temporary
            _mainImberPanelView.DidClickLoop += MainImberPanelView_DidClickLoop;
            _mainImberPanelView.DidPositionJump += MainImberPanelView_DidPositionJump;
            _mainImberPanelView.DidClickRestart += MainImberPanelView_DidClickRestart;
            _mainImberPanelView.DidClickPausePlay += MainImberPanelView_DidClickPausePlay;
            _mainImberPanelView.DidTimeSyncChange += MainImberPanelView_DidTimeSyncChange;
            _mainImberPanelView.DidChangeVisiblity += MainImberPanelView_DidChangeVisiblity;
            _mainImberPanelView.HandDidSwitchEvent += MainImberPanelView_DidHandSwitchEvent;
            _mainImberPanelView.DidPositionPreviewChange += MainImberPanelView_DidPositionPreviewChange;
            _mainImberPanelView.DidPositionTabVisibilityChange += MainImberPanelView_DidPositionTabVisibilityChange;
            _spectateAreaController.DidUpdatePlayerSpectatorPose += SpectateAreaController_DidUpdatePlayerSpectatorPose;
            _imberScrubber.DidCalculateNewTime += ImberScrubber_DidCalculateNewTime;
            _imberSpecsReporter.DidReport += ImberSpecsReporter_DidReport;
            _gamePause.didResumeEvent += GamePause_didResumeEvent;
            if (!_settings.Current.hasOpenedReplayUI) {
                CreateWatermark();
            }
        }

        private void MainImberPanelView_DidHandSwitchEvent(XRNode hand) {

            if (hand == XRNode.RightHand) {
                _settings.Current.leftHandedReplayUI = true;
            }
            if (hand == XRNode.LeftHand) {
                _settings.Current.leftHandedReplayUI = false;
            }

            _settings.Save();

            _imberUIPositionController.UpdateTrackingHand(hand);
        }

        private void GamePause_didResumeEvent() => _mainImberPanelView.playPauseText = "PAUSE";

        private void ImberSpecsReporter_DidReport(int fps, float leftSaberSpeed, float rightSaberSpeed) {

            if (_mainImberPanelView.didParse) {
                _mainImberPanelView.fps = fps;
                _mainImberPanelView.leftSaberSpeed = leftSaberSpeed * (_initialTimeScale / _audioTimeSyncController.timeScale);
                _mainImberPanelView.rightSaberSpeed = rightSaberSpeed * (_initialTimeScale / _audioTimeSyncController.timeScale);
            }
        }

        private void SpectateAreaController_DidUpdatePlayerSpectatorPose(Vector3 position, Quaternion rotation) {
            _imberUIPositionController.SetControllerOffset(position);
            _posePlayer.SetSpectatorOffset(position);
        }

        private void CreateWatermark() {

            GameObject _watermarkObject = new GameObject("Replay Prompt");
            _watermarkObject.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            _watermarkObject.transform.position = new Vector3(0f, 0.025f, -0.8f);
            _watermarkObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Canvas canvas = _watermarkObject.AddComponent<Canvas>();
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(100f, 50f);
            CurvedCanvasSettings curvedCanvasSettings = _watermarkObject.AddComponent<CurvedCanvasSettings>();
            curvedCanvasSettings.SetRadius(0f);
            CurvedTextMeshPro curvedTextMeshPro = (CurvedTextMeshPro)BeatSaberUI.CreateText((RectTransform)canvas.transform, "Double click left trigger to open Replay menu", new Vector2(0f, 0f));
            curvedTextMeshPro.alignment = TMPro.TextAlignmentOptions.Center;
            curvedTextMeshPro.color = new Color(0.95f, 0.95f, 0.95f);
        }

        #region UI Callbacks

        private void MainImberPanelView_DidPositionTabVisibilityChange(bool value) {

            if (value) {
                _spectateAreaController.AnimateTo(_mainImberPanelView.location);

            } else {
                _spectateAreaController.Dismiss();
            }
        }

        private void MainImberPanelView_DidPositionPreviewChange(string value) => _spectateAreaController.AnimateTo(value);

        private void MainImberPanelView_DidPositionJump() => _spectateAreaController.JumpToCallback(_mainImberPanelView.location);

        private void ImberScrubber_DidCalculateNewTime(float newTime) => _replayTimeSyncController.OverrideTime(newTime);

        private void MainImberPanelView_DidClickLoop() {

            _imberScrubber.loopMode = !_imberScrubber.loopMode;

            const string oddLoopID = "76561198114011987";
            string unloopText = "playerID" == oddLoopID || "replayPlayerID" == oddLoopID ? "ODDLOOP" : "UNLOOP";
            _mainImberPanelView.loopText = _imberScrubber.loopMode ? unloopText : "LOOP";
        }

        private void MainImberPanelView_DidClickRestart() => _replayTimeSyncController.OverrideTime(0f);

        private void MainImberPanelView_DidClickPausePlay() {

            if (_audioTimeSyncController.state == AudioTimeSyncController.State.Playing) {
                _replayTimeSyncController.CancelAllHitSounds();
                _mainImberPanelView.playPauseText = "PLAY";
                _audioTimeSyncController.Pause();
            } else if (_audioTimeSyncController.state == AudioTimeSyncController.State.Paused) {
                _mainImberPanelView.playPauseText = "PAUSE";
                _audioTimeSyncController.Resume();
            }
        }

        private void MainImberPanelView_DidTimeSyncChange(float value) => _replayTimeSyncController.OverrideTimeScale(value);

        private void MainImberPanelView_DidChangeVisiblity(bool value) {

            _imberUIPositionController.SetActiveState(value);
            _mainImberPanelView.visibility = value;
            _imberScrubber.visibility = value;
            if (!value) {
                _spectateAreaController.Dismiss();
            }
        }

        #endregion

        public void Dispose() {

            _gamePause.didResumeEvent -= GamePause_didResumeEvent;
            _imberSpecsReporter.DidReport -= ImberSpecsReporter_DidReport;
            _imberScrubber.DidCalculateNewTime -= ImberScrubber_DidCalculateNewTime;
            _spectateAreaController.DidUpdatePlayerSpectatorPose -= SpectateAreaController_DidUpdatePlayerSpectatorPose;
            _mainImberPanelView.DidPositionPreviewChange -= MainImberPanelView_DidPositionPreviewChange;
            _mainImberPanelView.HandDidSwitchEvent -= MainImberPanelView_DidHandSwitchEvent;
            _mainImberPanelView.DidChangeVisiblity -= MainImberPanelView_DidChangeVisiblity;
            _mainImberPanelView.DidTimeSyncChange -= MainImberPanelView_DidTimeSyncChange;
            _mainImberPanelView.DidClickPausePlay -= MainImberPanelView_DidClickPausePlay;
            _mainImberPanelView.DidClickRestart -= MainImberPanelView_DidClickRestart;
            _mainImberPanelView.DidPositionJump -= MainImberPanelView_DidPositionJump;
            _mainImberPanelView.DidClickLoop -= MainImberPanelView_DidClickLoop;
        }
    }
}
