using System;
using System.Linq;
using ScoreSaber.Core.Configuration;
using Tweening;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Replays.UI {
    internal class SpectateAreaController : ITickable, IDisposable {
        private static readonly int _colorID = Shader.PropertyToID("_Color");
        private readonly TimeTweeningManager _timeTweeningManager;
        private readonly GameNoteController.Pool _gameNoteControllerPool;
        private readonly SettingsService _settings;
        public event Action<Vector3, Quaternion> DidUpdatePlayerSpectatorPose;

        private GameNoteController _activeNote = null;
        private Quaternion _initialQuaternion;
        private Tween _movementTween = null;
        private Tween _statusTween = null;
        private bool _despawned = false;

        public SpectateAreaController(DiContainer diContainer, TimeTweeningManager timeTweeningManager, SettingsService settings) {

            _timeTweeningManager = timeTweeningManager;
            _settings = settings;
            _gameNoteControllerPool = diContainer.ResolveId<GameNoteController.Pool>(NoteData.GameplayType.Normal);
        }

        public void AnimateTo(string poseID) {

            if (!TryGetPose(poseID, out Settings.SpectatorPoseRoot pose))
                return;

            _statusTween?.Kill();
            if (_activeNote == null) {
                _activeNote = _gameNoteControllerPool.Spawn();
                _activeNote.enabled = false;
                _activeNote.transform.localScale = Vector3.zero;
                _initialQuaternion = _activeNote.noteTransform.localRotation;
                _activeNote.transform.SetLocalPositionAndRotation(pose.spectatorPose.ToVector3(), Quaternion.identity);
                _activeNote.noteTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(45f, 45f, 45f));

                ColorNoteVisuals visuals = _activeNote.GetComponent<ColorNoteVisuals>();
                visuals.showCircle = false;
                visuals.showArrow = false;
                var color = visuals._noteColor = Color.cyan.ColorWithAlpha(3f);

                foreach (var block in visuals._materialPropertyBlockControllers) {
                    block.materialPropertyBlock.SetColor(_colorID, color);
                    block.ApplyChanges();
                }

                _despawned = true;
            }

            if (_despawned) {
                _activeNote.gameObject.SetActive(true);
                _statusTween = new Vector3Tween(Vector3.zero, Vector3.one, UpdateNoteScale, 0.5f, EaseType.OutElastic);
                _timeTweeningManager.AddTween(_statusTween, _activeNote);
                _despawned = false;
            }

            _movementTween?.Kill();
            _activeNote.gameObject.SetActive(true);
            _movementTween = new Vector3Tween(_activeNote.transform.localPosition, pose.spectatorPose.ToVector3(),
                val => { _activeNote.transform.localPosition = val; }, 0.75f, EaseType.OutQuart);
            _timeTweeningManager.AddTween(_movementTween, _activeNote);
        }

        public void JumpToCallback(string poseID) {

            if (TryGetPose(poseID, out Settings.SpectatorPoseRoot pose)) {
                DidUpdatePlayerSpectatorPose?.Invoke(pose.spectatorPose.ToVector3(), Quaternion.identity);
            }
        }

        public void Dismiss() {

            if (_activeNote == null)
                return;

            _despawned = true;
            _statusTween?.Kill();
            _movementTween?.Kill();
            _statusTween = new Vector3Tween(Vector3.one, Vector3.zero, UpdateNoteScale, 0.5f, EaseType.OutQuart) {
                onCompleted = DespawnActiveNote
            };
            _timeTweeningManager.AddTween(_statusTween, _activeNote);
        }

        public void Tick() {

            if (_activeNote != null) {
                _activeNote.transform.Rotate(Vector3.up * 0.5f);
            }
        }

        private void UpdateNoteScale(Vector3 scale) {

            if (_activeNote != null) {
                _activeNote.transform.localScale = scale;
            }
        }

        private void DespawnActiveNote() {

            _despawned = true;
            _statusTween?.Kill();
            _movementTween?.Kill();
            _activeNote.gameObject.SetActive(false);
        }

        private bool TryGetPose(string poseID, out Settings.SpectatorPoseRoot pose) {

            pose = _settings.Current.spectatorPositions.FirstOrDefault(sp => sp.name == poseID);
            return pose.name != null;
        }

        public void Dispose() {

            if (_activeNote != null) {
                _timeTweeningManager.KillAllTweens(_activeNote);
            }
        }
    }
}
