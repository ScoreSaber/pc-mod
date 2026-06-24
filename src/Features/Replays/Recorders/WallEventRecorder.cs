using ScoreSaber.Features.Replays.Format;
using ScoreSaber.Features.Live.Replay;
using System;
using System.Collections.Generic;
using Zenject;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class WallEventRecorder : TimeSynchronizer, IInitializable, ITickable, IDisposable {
        private const int InitialWallEventCapacity = 16;

        private readonly PlayerHeadAndObstacleInteraction _playerHeadAndObstacleInteraction;
        private readonly IGameEnergyCounter _gameEnergyCounter;
        private readonly LiveReplayStreamingService _liveReplayStreamingService;
        private readonly List<WallEvent> _wallEvents;
        private readonly List<int> _openContactIndexes;
        private bool _headInObstacle;

        public WallEventRecorder([InjectOptional] PlayerHeadAndObstacleInteraction playerHeadAndObstacleInteraction, [InjectOptional] IGameEnergyCounter gameEnergyCounter, LiveReplayStreamingService liveReplayStreamingService) {

            _playerHeadAndObstacleInteraction = playerHeadAndObstacleInteraction;
            _gameEnergyCounter = gameEnergyCounter;
            _liveReplayStreamingService = liveReplayStreamingService;
            _wallEvents = new List<WallEvent>(InitialWallEventCapacity);
            _openContactIndexes = new List<int>(InitialWallEventCapacity);
        }

        public void Initialize() {

            if (_playerHeadAndObstacleInteraction != null) {
                _headInObstacle = _playerHeadAndObstacleInteraction.playerHeadIsInObstacle;
                _playerHeadAndObstacleInteraction.headDidEnterObstacleEvent += HeadDidEnterObstacleEvent;
            }
        }

        public void Tick() {

            if (_playerHeadAndObstacleInteraction == null) {
                return;
            }

            bool headInObstacle = _playerHeadAndObstacleInteraction.playerHeadIsInObstacle;
            if (_headInObstacle && !headInObstacle) {
                CloseOpenContacts();
            }
            _headInObstacle = headInObstacle;
        }

        public void Dispose() {

            if (_playerHeadAndObstacleInteraction != null) {
                _playerHeadAndObstacleInteraction.headDidEnterObstacleEvent -= HeadDidEnterObstacleEvent;
            }
        }

        public List<WallEvent> Export() {

            CloseOpenContacts();
            return _wallEvents;
        }

        private void HeadDidEnterObstacleEvent(ObstacleController obstacleController) {

            if (obstacleController == null) {
                return;
            }

            ObstacleData obstacle = obstacleController.obstacleData;
            _wallEvents.Add(new WallEvent {
                Time = audioTimeSyncController.songTime,
                ExitTime = audioTimeSyncController.songTime,
                Energy = CurrentEnergy(),
                ObstacleTime = obstacle.time,
                ObstacleDuration = obstacle.duration,
                LineIndex = obstacle.lineIndex,
                LineLayer = (int)obstacle.lineLayer,
                Width = obstacle.width,
                Height = obstacle.height
            });
            _openContactIndexes.Add(_wallEvents.Count - 1);
            _headInObstacle = true;
        }

        private void CloseOpenContacts() {

            if (_openContactIndexes.Count == 0) {
                return;
            }

            float exitTime = audioTimeSyncController.songTime;
            float energy = CurrentEnergy();
            foreach (int index in _openContactIndexes) {
                WallEvent wallEvent = _wallEvents[index];
                wallEvent.ExitTime = Math.Max(wallEvent.Time, exitTime);
                wallEvent.Energy = energy;
                _wallEvents[index] = wallEvent;
                _liveReplayStreamingService.RecordWall(wallEvent);
            }
            _openContactIndexes.Clear();
        }

        private float CurrentEnergy() => _gameEnergyCounter != null ? _gameEnergyCounter.energy : 0f;
    }
}
