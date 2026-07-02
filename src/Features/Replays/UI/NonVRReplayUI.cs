using ScoreSaber.Core.Configuration;
using ScoreSaber.Core.Compat;
using ScoreSaber.Features.Replays.Format;
using ScoreSaber.Features.Replays.Playback;
using System.Linq;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Replays.UI {
    internal class NonVRReplayUI : MonoBehaviour {
        [Inject] private readonly AudioTimeSyncController _audioTimeSyncController = null;
        [Inject] private readonly PosePlayer _posePlayer = null;
        [Inject] private readonly SaberManager _saberManager = null;
        [Inject] private readonly ReplayFile _file = null;
        [Inject] private readonly SettingsService _settings = null;

        private GUIStyle _headerStyle;

        private int _currentPosition = 0;
        const int _offset = 16;
        const int _headerOffset = 20;
        private float _initialTimeScale;

        private string _fpsLine = "Player's FPS: 0";
        private string _leftSaberLine = "Left Saber Speed:";
        private string _rightSaberLine = "Right Saber Speed:";
        private string _songTimeLine = "Current Song Time: 0:00";
        private string _timeScaleLine = "Current Time Scale:";
        private int _lastFps = -1;
        private int _lastSongTimeSecond = -1;
        private float _lastTimeScale = -1f;

        protected void Start() {

            _headerStyle = new GUIStyle();
            _headerStyle.fontSize = 16;
            _headerStyle.normal.textColor = Color.white;
            _initialTimeScale = _file.noteKeyframes.Count > 0 ? _file.noteKeyframes[0].TimeSyncTimescale : 1f;
            _posePlayer.DidUpdatePose += PosePlayer_DidUpdatePose;
        }

        protected void OnDestroy() {

            _posePlayer.DidUpdatePose -= PosePlayer_DidUpdatePose;
        }

        private void PosePlayer_DidUpdatePose(VRPoseGroup pose) {

            if (_settings.Current.hideReplayUI) {
                return;
            }

            if (pose.FPS != _lastFps) {
                _lastFps = pose.FPS;
                _fpsLine = $"Player's FPS: {pose.FPS}";
            }

            float timeScaleRatio = _initialTimeScale / _audioTimeSyncController.timeScale;
            _leftSaberLine = $"Left Saber Speed: {_saberManager.leftSaber.GetMovementDataForLogic().bladeSpeed * timeScaleRatio:0.0} m/s";
            _rightSaberLine = $"Right Saber Speed: {_saberManager.rightSaber.GetMovementDataForLogic().bladeSpeed * timeScaleRatio:0.0} m/s";
        }

        protected void OnGUI() {

            if (!_settings.Current.hideReplayUI) {
                _currentPosition = 0;
                DrawLabel("Replay Controls -", header: true);
                DrawLabel("Pause: Space");
                DrawLabel("Seek: 1-9 OR Arrow Keys");
                DrawLabel("Increase Time Scale: +");
                DrawLabel("Decrease Time Scale: -");
                DrawLabel("Hide Sabers: H");
                DrawLabel("Hide Desktop Replay UI: C");
                DrawLabel("Replay Player Status -", header: true);

                int songSecond = (int)_audioTimeSyncController.songTime;
                if (songSecond != _lastSongTimeSecond) {
                    _lastSongTimeSecond = songSecond;
                    _songTimeLine = $"Current Song Time: {songSecond / 60}:{songSecond % 60:00}";
                }

                float timeScale = _audioTimeSyncController.timeScale;
                if (timeScale != _lastTimeScale) {
                    _lastTimeScale = timeScale;
                    _timeScaleLine = $"Current Time Scale: {timeScale:P0}";
                }

                DrawLabel(_songTimeLine);
                DrawLabel(_timeScaleLine);
                DrawLabel(_fpsLine);
                DrawLabel(_leftSaberLine);
                DrawLabel(_rightSaberLine);
            }
        }

        protected void Update() {

            if (Input.GetKeyDown(KeyCode.C)) {
                _settings.Current.hideReplayUI = !_settings.Current.hideReplayUI;
            }
        }

        private void DrawLabel(string text, bool header = false) {

            if (header) {
                _currentPosition += _headerOffset;
                GUI.Label(new Rect(10, _currentPosition, 300, 20), text, _headerStyle);
            } else {
                _currentPosition += _offset;
                GUI.Label(new Rect(10, _currentPosition, 300, 20), text);
            }
        }
    }
}
