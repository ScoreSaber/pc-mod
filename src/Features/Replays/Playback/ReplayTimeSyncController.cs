using IPA.Utilities;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Replays.Playback {
    internal class ReplayTimeSyncController : TimeSynchronizer, ITickable {
        private static readonly KeyCode[] TimeJumpKeys = {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9,
            KeyCode.Alpha0
        };

        private readonly List<IScroller> _scrollers;
        private readonly AudioManagerSO _audioManagerSO;
        private AudioTimeSyncController.InitData _audioInitData;
        private BasicBeatmapObjectManager _basicBeatmapObjectManager;
        private NoteCutSoundEffectManager _noteCutSoundEffectManager;
        private BeatmapCallbacksController.InitData _callbackInitData;
        private BeatmapCallbacksController _beatmapObjectCallbackController;
        private readonly BeatmapObjectSpawnController _beatmapObjectSpawnController;
        private bool _paused;

        public ReplayTimeSyncController(List<IScroller> scrollers, BasicBeatmapObjectManager basicBeatmapObjectManager, NoteCutSoundEffectManager noteCutSoundEffectManager, BeatmapObjectSpawnController beatmapObjectSpawnController, AudioTimeSyncController.InitData audioInitData, BeatmapCallbacksController.InitData initData, BeatmapCallbacksController beatmapObjectCallbackController) {
            _scrollers = scrollers;
            _callbackInitData = initData;
            _audioInitData = audioInitData;
            _basicBeatmapObjectManager = basicBeatmapObjectManager;
            _noteCutSoundEffectManager = noteCutSoundEffectManager;
            _beatmapObjectSpawnController = beatmapObjectSpawnController;
            _beatmapObjectCallbackController = beatmapObjectCallbackController;
            _audioManagerSO = Accessors.AudioManager(ref noteCutSoundEffectManager);
        }

        public void Tick() {
            int index = TimeJumpKeyIndex();
            if (index >= 0) {
                OverrideTime(audioTimeSyncController.songLength * (index * 0.1f));
            }

            if (Input.GetKeyDown(KeyCode.Minus) && audioTimeSyncController.timeScale > 0.1f) {
                OverrideTimeScale(audioTimeSyncController.timeScale - 0.1f);
            }

            if (Input.GetKeyDown(KeyCode.Equals) && audioTimeSyncController.timeScale < 2.0f) {
                OverrideTimeScale(audioTimeSyncController.timeScale + 0.1f);
            }

            if (Input.GetKeyDown(KeyCode.R)) {
                OverrideTime(0f);
            }

            if (Input.GetKeyDown(KeyCode.Space)) {
                if (_paused) {
                    audioTimeSyncController.Resume();
                } else {
                    CancelAllHitSounds();
                    audioTimeSyncController.Pause();
                }
                _paused = !_paused;
            }
        }

        private static int TimeJumpKeyIndex() {
            for (int i = 0; i < TimeJumpKeys.Length; i++) {
                if (Input.GetKeyDown(TimeJumpKeys[i])) {
                    return i;
                }
            }

            return -1;
        }

        private void UpdateTimes() {
            foreach (var scroller in _scrollers)
                scroller.TimeUpdate(audioTimeSyncController.songTime);
        }

        public void OverrideTime(float time) {

            if (Mathf.Abs(time - audioTimeSyncController.songTime) <= 0.25f)
                return;

            var _audioTimeSyncController = audioTimeSyncController; // UMBRAMEGALUL
            HarmonyPatches.CutSoundEffectOverride.Buffer = true;
            CancelAllHitSounds();

            // Forcibly enabling all the note/obstacle components to ensure their dissolve coroutine executes (it no likey when game pausey).
            // TODO: do we have to do this for arcs aswell?
            ResetBeatmapObjects(Accessors.GameNotePool(ref _basicBeatmapObjectManager).activeItems);
            ResetBeatmapObjects(Accessors.BurstSliderHeadNotePool(ref _basicBeatmapObjectManager).activeItems);
            ResetBeatmapObjects(Accessors.BurstSliderNotePool(ref _basicBeatmapObjectManager).activeItems);
            ResetBeatmapObjects(Accessors.BombNotePool(ref _basicBeatmapObjectManager).activeItems);
            ResetBeatmapObjects(_basicBeatmapObjectManager.activeObstacleControllers);

            var previousState = audioTimeSyncController.state;

            audioTimeSyncController.Pause();
            audioTimeSyncController.SeekTo(time / audioTimeSyncController.timeScale);

            if (previousState == AudioTimeSyncController.State.Playing)
                audioTimeSyncController.Resume();

            Accessors.InitialStartFilterTime(ref _callbackInitData) = time;
            Accessors.CallbackStartFilterTime(ref _beatmapObjectCallbackController) = time;

            foreach (var callback in Accessors.CallbacksInTime(ref _beatmapObjectCallbackController)) {

                if (callback.Value.lastProcessedNode != null && callback.Value.lastProcessedNode.Value.time > time)
                    callback.Value.lastProcessedNode = null;
            }

            Accessors.AudioSongTime(ref _audioTimeSyncController) = time;

            audioTimeSyncController.Update();
            UpdateTimes();
        }

        private static void ResetBeatmapObjects<T>(IEnumerable<T> controllers) where T : Behaviour, IBeatmapObjectController {
            foreach (var controller in controllers) {
                controller.Hide(false);
                controller.Pause(false);
                controller.enabled = true;
                controller.gameObject.SetActive(true);
                controller.Dissolve(0f);
            }
        }

        public void OverrideTimeScale(float newScale) {

            CancelAllHitSounds();
            var _audioTimeSyncController = audioTimeSyncController; // UMBRAMEGALUL
            Accessors.AudioSource(ref _audioTimeSyncController).pitch = newScale;

            Accessors.AudioTimeScale(ref _audioTimeSyncController) = newScale;
            Accessors.AudioStartOffset(ref _audioTimeSyncController)
                = (Time.timeSinceLevelLoad * _audioTimeSyncController.timeScale) - (_audioTimeSyncController.songTime + _audioInitData.songTimeOffset);

            _audioManagerSO.musicPitch = 1f / newScale;
            _audioTimeSyncController.Update();
        }

        public void CancelAllHitSounds() {

            var activeItems = Accessors.NoteCutPool(ref _noteCutSoundEffectManager).activeItems;
            for (int i = 0; i < activeItems.Count; i++) {
                var effect = activeItems[i];
                if (effect.isActiveAndEnabled)
                    effect.StopPlayingAndFinish();
            }
            _noteCutSoundEffectManager.SetField("_prevNoteATime", -1f);
            _noteCutSoundEffectManager.SetField("_prevNoteBTime", -1f);
        }
    }
}
