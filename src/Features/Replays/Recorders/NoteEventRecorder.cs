using ScoreSaber.Features.Live.Replay;
using ScoreSaber.Features.Replays;
using System;
using System.Collections.Generic;
using Zenject;
using ScoreSaber.Features.Replays.Format;
using SiraUtil.Affinity;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class NoteEventRecorder : TimeSynchronizer, IInitializable, IDisposable, IAffinity {
        private const int InitialNoteEventCapacity = 4096;
        private const int InitialCutInfoCapacity = 128;

        private readonly List<NoteEvent> _noteKeyframes;
        private readonly ScoreController _scoreController;
        private readonly Dictionary<NoteData, NoteCutInfo> _collectedBadCutInfos;
        private readonly LiveReplayStreamingService _liveReplayStreamingService;

        public NoteEventRecorder(ScoreController scoreController, LiveReplayStreamingService liveReplayStreamingService) {

            _scoreController = scoreController;
            _liveReplayStreamingService = liveReplayStreamingService;
            _noteKeyframes = new List<NoteEvent>(InitialNoteEventCapacity);
            _collectedBadCutInfos = new Dictionary<NoteData, NoteCutInfo>(InitialCutInfoCapacity);
        }

        public void Initialize() {

            _scoreController.scoringForNoteFinishedEvent += ScoreController_scoringForNoteFinishedEvent;
        }

        private void ScoreController_scoringForNoteFinishedEvent(ScoringElement element) {

            var noteData = element.noteData;
            NoteID noteID = new NoteID() {
                Time = noteData.time,
                LineIndex = noteData.lineIndex,
                LineLayer = (int)noteData.noteLineLayer,
                ColorType = (int)noteData.colorType,
                CutDirection = (int)noteData.cutDirection,
                GameplayType = (int)noteData.gameplayType,
                ScoringType = (int)noteData.scoringType,
                CutDirectionAngleOffset = noteData.cutDirectionAngleOffset,
            };

            if (element is GoodCutScoringElement goodCut) {

                var noteCutInfo = goodCut.cutScoreBuffer.noteCutInfo;
                _collectedBadCutInfos.Remove(noteData);
                float cutTime = noteData.time - noteCutInfo.timeDeviation;

                NoteEvent noteEvent = CreateCutEvent(noteID, NoteEventType.GoodCut, noteCutInfo, goodCut.cutScoreBuffer.beforeCutSwingRating, goodCut.cutScoreBuffer.afterCutSwingRating, cutTime);
                _noteKeyframes.Add(noteEvent);
                _liveReplayStreamingService.RecordNote(noteEvent);

            } else if (element is BadCutScoringElement badCut) {

                var badCutEventType = noteData.colorType == ColorType.None ? NoteEventType.Bomb : NoteEventType.BadCut;
                if (!_collectedBadCutInfos.TryGetValue(badCut.noteData, out NoteCutInfo noteCutInfo)) {
                    Plugin.Log.Debug("Skipping replay bad cut event because cut info was not collected");
                    return;
                }
                _collectedBadCutInfos.Remove(badCut.noteData);
                float cutTime = noteData.time - noteCutInfo.timeDeviation;

                NoteEvent noteEvent = CreateCutEvent(noteID, badCutEventType, noteCutInfo, 0f, 0f, cutTime);
                _noteKeyframes.Add(noteEvent);
                _liveReplayStreamingService.RecordNote(noteEvent);

            } else if (noteData.colorType != ColorType.None /* not bomb */ && element is MissScoringElement) {

                var noteEvent = new NoteEvent() {

                    NoteID = noteID,
                    EventType = NoteEventType.Miss,
                    CutPoint = VRPosition.None(),
                    CutNormal = VRPosition.None(),
                    SaberDirection = VRPosition.None(),
                    SaberType = (int)noteData.colorType,
                    DirectionOK = false, CutDirectionDeviation = 0f,
                    SaberSpeed = 0f,
                    CutAngle = 0f,
                    CutDistanceToCenter = 0f,
                    BeforeCutRating = 0f,
                    AfterCutRating = 0f,
                    Time = audioTimeSyncController.songTime,
                    UnityTimescale = UnityEngine.Time.timeScale,
                    TimeSyncTimescale = audioTimeSyncController.timeScale,

                    // I couldn't find where to grab these for misses
                    TimeDeviation = 0f,
                    WorldRotation = new VRRotation(),
                    InverseWorldRotation = new VRRotation(),
                    NoteRotation = new VRRotation(),
                    NotePosition = new VRPosition(),
                };
                _noteKeyframes.Add(noteEvent);
                _liveReplayStreamingService.RecordNote(noteEvent);
            }
        }

        private NoteEvent CreateCutEvent(NoteID noteID, NoteEventType eventType, NoteCutInfo noteCutInfo, float beforeCutRating, float afterCutRating, float cutTime) {
            return new NoteEvent() {
                NoteID = noteID,
                EventType = eventType,
                CutPoint = noteCutInfo.cutPoint.Convert(),
                CutNormal = noteCutInfo.cutNormal.Convert(),
                SaberDirection = noteCutInfo.saberDir.Convert(),
                SaberType = (int)noteCutInfo.saberType,
                DirectionOK = noteCutInfo.directionOK,
                CutDirectionDeviation = noteCutInfo.cutDirDeviation,
                SaberSpeed = noteCutInfo.saberSpeed,
                CutAngle = noteCutInfo.cutAngle,
                CutDistanceToCenter = noteCutInfo.cutDistanceToCenter,
                BeforeCutRating = beforeCutRating,
                AfterCutRating = afterCutRating,
                Time = cutTime,
                UnityTimescale = UnityEngine.Time.timeScale,
                TimeSyncTimescale = audioTimeSyncController.timeScale,

                TimeDeviation = noteCutInfo.timeDeviation,
                WorldRotation = noteCutInfo.worldRotation.Convert(),
                InverseWorldRotation = noteCutInfo.inverseWorldRotation.Convert(),
                NoteRotation = noteCutInfo.noteRotation.Convert(),
                NotePosition = noteCutInfo.notePosition.Convert(),
            };
        }

        [AffinityPrefix, AffinityPatch(typeof(ScoreController), nameof(ScoreController.HandleNoteWasCut))]
        protected void BadCutInfoCollector(NoteController noteController, in NoteCutInfo noteCutInfo) {

            _collectedBadCutInfos[noteController.noteData] = noteCutInfo;
        }

        public void Dispose() {

            _scoreController.scoringForNoteFinishedEvent -= ScoreController_scoringForNoteFinishedEvent;
        }

        public List<NoteEvent> Export() {

            return _noteKeyframes;
        }
    }
}
