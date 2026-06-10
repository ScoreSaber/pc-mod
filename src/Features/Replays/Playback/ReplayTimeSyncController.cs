using IPA.Utilities;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
#if BEAT_SABER_1_42_0
using AudioTimeSourceState = IAudioTimeSource.State;
#else
// older versions use AudioManagerSO and AudioTimeSyncController.State here
using AudioManager = AudioManagerSO;
using AudioTimeSourceState = AudioTimeSyncController.State;
#endif

namespace ScoreSaber.Features.Replays.Playback {
    internal class ReplayTimeSyncController : TimeSynchronizer, ITickable {
        private static readonly FieldAccessor<BeatmapCallbacksController.InitData, float>.Accessor InitialStartFilterTime =
            FieldAccessor<BeatmapCallbacksController.InitData, float>.GetAccessor("startFilterTime");
        private static readonly FieldAccessor<BeatmapCallbacksController, float>.Accessor CallbackStartFilterTime =
            FieldAccessor<BeatmapCallbacksController, float>.GetAccessor("_startFilterTime");

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
        private readonly AudioManager _audioManager;
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
            _audioManager = noteCutSoundEffectManager._audioManager;
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
            ResetBeatmapObjects(_basicBeatmapObjectManager._basicGameNotePoolContainer.activeItems);
            ResetBeatmapObjects(_basicBeatmapObjectManager._burstSliderHeadGameNotePoolContainer.activeItems);
            ResetBeatmapObjects(_basicBeatmapObjectManager._burstSliderGameNotePoolContainer.activeItems);
            ResetBeatmapObjects(_basicBeatmapObjectManager._bombNotePoolContainer.activeItems);
            ResetBeatmapObjects(_basicBeatmapObjectManager.activeObstacleControllers);

            var previousState = audioTimeSyncController.state;

            audioTimeSyncController.Pause();
            audioTimeSyncController.SeekTo(time / audioTimeSyncController.timeScale);

            if (previousState == AudioTimeSourceState.Playing)
                audioTimeSyncController.Resume();

            InitialStartFilterTime(ref _callbackInitData) = time;
            CallbackStartFilterTime(ref _beatmapObjectCallbackController) = time;

            foreach (var callback in _beatmapObjectCallbackController._callbacksInTimes) {

                if (callback.Value.lastProcessedNode != null && callback.Value.lastProcessedNode.Value.time > time)
                    callback.Value.lastProcessedNode = null;
            }

            _audioTimeSyncController._songTime = time;

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
            _audioTimeSyncController._audioSource.pitch = newScale;

            _audioTimeSyncController._timeScale = newScale;
            _audioTimeSyncController._audioStartTimeOffsetSinceStart
                = (Time.timeSinceLevelLoad * _audioTimeSyncController.timeScale) - (_audioTimeSyncController.songTime + _audioInitData.songTimeOffset);

            _audioManager.musicPitch = 1f / newScale;
            _audioTimeSyncController.Update();
        }

        public void CancelAllHitSounds() {

            var activeItems = _noteCutSoundEffectManager._noteCutSoundEffectPoolContainer.activeItems;
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
