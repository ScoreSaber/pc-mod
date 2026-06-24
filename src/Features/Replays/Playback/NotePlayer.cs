using IPA.Utilities;
using ScoreSaber.Core.Compat;
using ScoreSaber.Features.Replays.Format;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Replays.Playback {
    internal class NotePlayer : TimeSynchronizer, ITickable, IScroller, IAffinity {
        private int _nextIndex = 0;
        private readonly SiraLog _siraLog;
        private readonly SaberManager _saberManager;
        private readonly NoteEvent[] _sortedNoteEvents;
        private readonly ReplayFile _replayFile;
        private readonly MemoryPoolContainer<GameNoteController> _gameNotePool;
        private readonly MemoryPoolContainer<GameNoteController> _burstSliderHeadNotePool;
        private readonly MemoryPoolContainer<BurstSliderGameNoteController> _burstSliderNotePool;
        private readonly MemoryPoolContainer<BombNoteController> _bombNotePool;

        private readonly Dictionary<NoteCutInfo, NoteEvent> _recognizedNoteCutInfos = new Dictionary<NoteCutInfo, NoteEvent>();

        public NotePlayer(SiraLog siraLog, ReplayFile file, SaberManager saberManager, BasicBeatmapObjectManager basicBeatmapObjectManager) {

            _siraLog = siraLog;
            _saberManager = saberManager;
            _gameNotePool = basicBeatmapObjectManager._basicGameNotePoolContainer;
            _burstSliderHeadNotePool = basicBeatmapObjectManager._burstSliderHeadGameNotePoolContainer;
            _burstSliderNotePool = basicBeatmapObjectManager._burstSliderGameNotePoolContainer;
            _bombNotePool = basicBeatmapObjectManager._bombNotePoolContainer;
            _sortedNoteEvents = file.noteKeyframes.OrderBy(nk => nk.Time).ToArray();
            _replayFile = file;
        }

        public void Tick() {

            while (_nextIndex < _sortedNoteEvents.Length && audioTimeSyncController.songTime >= _sortedNoteEvents[_nextIndex].Time) {

                NoteEvent activeEvent = _sortedNoteEvents[_nextIndex++];
                ProcessEvent(activeEvent);
            }
        }

        private void ProcessEvent(NoteEvent activeEvent) {

            if (activeEvent.EventType == NoteEventType.GoodCut || activeEvent.EventType == NoteEventType.BadCut) {
                if (HandleActiveEvent(activeEvent, _gameNotePool.activeItems) ||
                    HandleActiveEvent(activeEvent, _burstSliderHeadNotePool.activeItems) ||
                    HandleActiveEvent(activeEvent, _burstSliderNotePool.activeItems)) {
                    return;
                }
            } else if (activeEvent.EventType == NoteEventType.Miss) {
                if (HandleMissEvent(activeEvent, _gameNotePool.activeItems) ||
                    HandleMissEvent(activeEvent, _burstSliderHeadNotePool.activeItems) ||
                    HandleMissEvent(activeEvent, _burstSliderNotePool.activeItems)) {
                    return;
                }
            } else if (activeEvent.EventType == NoteEventType.Bomb) {
                HandleActiveEvent(activeEvent, _bombNotePool.activeItems);
            }
        }

        private bool HandleActiveEvent<T>(NoteEvent activeEvent, IEnumerable<T> controllers) where T : NoteController {
            foreach (T controller in controllers) {
                if (HandleEvent(activeEvent, controller)) {
                    return true;
                }
            }

            return false;
        }

        private bool HandleMissEvent<T>(NoteEvent activeEvent, IEnumerable<T> controllers) where T : NoteController {
            foreach (T controller in controllers) {
                if (DoesNoteMatchID(activeEvent.NoteID, controller.noteData)) {
                    HarmonyPatches.ReplayNoteMissEventGuard.Allow(controller);
                    try {
                        controller.InvokeMethod<object, NoteController>("SendNoteWasMissedEvent");
                    } finally {
                        HarmonyPatches.ReplayNoteMissEventGuard.Clear(controller);
                    }
                    return true;
                }
            }

            return false;
        }

        private bool HandleEvent(NoteEvent activeEvent, NoteController noteController) {

            if (DoesNoteMatchID(activeEvent.NoteID, noteController.noteData)) {
                Saber correctSaber = noteController.noteData.colorType == ColorType.ColorA ? _saberManager.leftSaber : _saberManager.rightSaber;
                var noteTransform = noteController.noteTransform;

                NoteCutInfo noteCutInfo = new NoteCutInfo(noteController.noteData,
                    activeEvent.SaberSpeed > 2f,
                    activeEvent.DirectionOK,
                    activeEvent.SaberType == (int)correctSaber.saberType,
                    false,
                    activeEvent.SaberSpeed,
                    activeEvent.SaberDirection.Convert(),
                    noteController.noteData.colorType == ColorType.ColorA ? SaberType.SaberA : SaberType.SaberB,
                    noteController.noteData.time - activeEvent.Time,
                    activeEvent.CutDirectionDeviation,
                    activeEvent.CutPoint.Convert(),
                    activeEvent.CutNormal.Convert(),
                    activeEvent.CutDistanceToCenter,
                    activeEvent.CutAngle,

                    noteController.worldRotation,
                    noteController.inverseWorldRotation,
                    noteTransform.rotation,
                    noteTransform.position,

                    correctSaber.GetMovementDataForLogic()
                );

                _recognizedNoteCutInfos.Add(noteCutInfo, activeEvent);
                noteController.InvokeMethod<object, NoteController>("SendNoteWasCutEvent", noteCutInfo);
                return true;
            }
            return false;
        }

        bool DoesNoteMatchID(NoteID id, NoteData noteData) {

            if (!Mathf.Approximately(id.Time, noteData.time) || id.LineIndex != noteData.lineIndex || id.LineLayer != (int)noteData.noteLineLayer || id.ColorType != (int)noteData.colorType || id.CutDirection != (int)noteData.cutDirection)
                return false;

            if (id.GameplayType is int gameplayType && gameplayType != (int)noteData.gameplayType)
                return false;

            if (!id.MatchesScoringType(noteData.scoringType, _replayFile.metadata.GameVersion))
                return false;

            if (id.CutDirectionAngleOffset is float cutDirectionAngleOffset && !Mathf.Approximately(cutDirectionAngleOffset, noteData.cutDirectionAngleOffset))
                return false;

            return true;
        }

        [AffinityPostfix, AffinityPatch(typeof(GoodCutScoringElement), nameof(GoodCutScoringElement.Init))]
        protected void ForceCompleteGoodScoringElements(GoodCutScoringElement __instance, NoteCutInfo noteCutInfo, CutScoreBuffer ____cutScoreBuffer) {

            // Just in case someone else is creating their own scoring elements, we want to ensure that we're only force completing ones we know we've created
            if (!_recognizedNoteCutInfos.TryGetValue(noteCutInfo, out var activeEvent))
                return;

            _recognizedNoteCutInfos.Remove(noteCutInfo);

            if (!__instance.isFinished) {

                var ratingCounter = ____cutScoreBuffer._saberSwingRatingCounter;

                // Supply the rating counter with the proper cut ratings
                ratingCounter._afterCutRating = activeEvent.AfterCutRating;
                ratingCounter._beforeCutRating = activeEvent.BeforeCutRating;

                // Then immediately finish it
                ____cutScoreBuffer.HandleSaberSwingRatingCounterDidFinish(ratingCounter);

                __instance.isFinished = true;
            }
        }

        public void TimeUpdate(float newTime) {

            _nextIndex = ReplayTimeSearch.CountAtOrBefore(_sortedNoteEvents, newTime, noteEvent => noteEvent.Time);
        }
    }
}
