using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using Zenject;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class EnergyEventRecorder : TimeSynchronizer, IInitializable, IDisposable {
        private const int InitialEnergyEventCapacity = 128;

        private readonly List<EnergyEvent> _energyKeyframes;
        private readonly IGameEnergyCounter _gameEnergyCounter;

        public EnergyEventRecorder(IGameEnergyCounter gameEnergyCounter) {

            _gameEnergyCounter = gameEnergyCounter;
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

            _energyKeyframes.Add(new EnergyEvent() { Energy = energy, Time = audioTimeSyncController.songTime });
        }

        public List<EnergyEvent> Export() {

            return _energyKeyframes;
        }

    }
}
