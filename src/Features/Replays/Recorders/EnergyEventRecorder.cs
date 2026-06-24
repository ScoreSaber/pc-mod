using ScoreSaber.Features.Live.Replay;
using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using Zenject;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class EnergyEventRecorder : TimeSynchronizer, IInitializable, IDisposable {
        private const int InitialEnergyEventCapacity = 128;

        private readonly List<EnergyEvent> _energyKeyframes;
        private readonly IGameEnergyCounter _gameEnergyCounter;
        private readonly LiveReplayStreamingService _liveReplayStreamingService;

        public EnergyEventRecorder(IGameEnergyCounter gameEnergyCounter, LiveReplayStreamingService liveReplayStreamingService) {

            _gameEnergyCounter = gameEnergyCounter;
            _liveReplayStreamingService = liveReplayStreamingService;
            _energyKeyframes = new List<EnergyEvent>(InitialEnergyEventCapacity);
        }

        public void Initialize() {

            if (_gameEnergyCounter != null) {
                _gameEnergyCounter.gameEnergyDidChangeEvent += GameEnergyCounter_gameEnergyDidChangeEvent;
            }
        }

        public void Dispose() {

            if (_gameEnergyCounter != null) {
                _gameEnergyCounter.gameEnergyDidChangeEvent -= GameEnergyCounter_gameEnergyDidChangeEvent;
            }
        }

        private void GameEnergyCounter_gameEnergyDidChangeEvent(float energy) {

            var energyEvent = new EnergyEvent() { Energy = energy, Time = audioTimeSyncController.songTime };
            _energyKeyframes.Add(energyEvent);
            _liveReplayStreamingService.RecordEnergy(energyEvent);
        }

        public List<EnergyEvent> Export() {

            return _energyKeyframes;
        }

    }
}
