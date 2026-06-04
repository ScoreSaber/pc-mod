using IPA.Utilities;
using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Replays.Playback {
    internal class EnergyPlayer : TimeSynchronizer, IScroller {
        private GameEnergyCounter _gameEnergyCounter;
        private GameEnergyUIPanel _gameEnergyUIPanel;
        private PlayerDataModel _playerDataModel;
        private readonly List<EnergyEvent> _energyEvents;

        public EnergyPlayer(ReplayFile file, GameEnergyCounter gameEnergyCounter, PlayerDataModel playerDataModel, DiContainer container) {

            _gameEnergyCounter = gameEnergyCounter;
            _playerDataModel = playerDataModel;
            _gameEnergyUIPanel = container.TryResolve<GameEnergyUIPanel>();
            _energyEvents = file.energyKeyframes;
        }

        public void TimeUpdate(float newTime) {

            int nextIndex = ReplayTimeSearch.CountAtOrBefore(_energyEvents, newTime, energyEvent => energyEvent.Time);
            float energy = nextIndex > 0 ? _energyEvents[nextIndex - 1].Energy : 0.5f;
            UpdateEnergy(energy);
        }

        private void UpdateEnergy(float energy) {

            bool isFailingEnergy = energy <= Mathf.Epsilon;

            bool noFail = _gameEnergyCounter.noFail;
            Accessors.NoFailPropertyUpdater(ref _gameEnergyCounter, false);
            Accessors.DidReachZero(ref _gameEnergyCounter) = isFailingEnergy;
            _gameEnergyCounter.ProcessEnergyChange(energy);
            Accessors.NextEnergyChange(ref _gameEnergyCounter) = 0;
            Accessors.ActiveEnergy(ref _gameEnergyCounter, energy);
            Accessors.NoFailPropertyUpdater(ref _gameEnergyCounter, noFail);

            if (_gameEnergyUIPanel != null && !_playerDataModel.playerData.playerSpecificSettings.noTextsAndHuds) {
                _gameEnergyUIPanel.Init();
                var director = Accessors.Director(ref _gameEnergyUIPanel);
                director.Stop();
                director.Evaluate();
                Accessors.EnergyBar(ref _gameEnergyUIPanel).enabled = !isFailingEnergy;
            }

            FieldAccessor<GameEnergyCounter, Action<float>>.Get(_gameEnergyCounter, "gameEnergyDidChangeEvent").Invoke(energy);
        }
    }
}
