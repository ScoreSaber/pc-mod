using IPA.Utilities;
using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ScoreSaber.Features.Replays.Playback {
    internal class EnergyPlayer : TimeSynchronizer, IScroller {
        private GameEnergyCounter _gameEnergyCounter;
        private GameEnergyUIPanel _gameEnergyUIPanel;
        private PlayerDataModel _playerDataModel;
        private readonly List<EnergyEvent> _energyEvents;
        private readonly Dictionary<RectTransform, Vector3> _initialEnergyIconPositions = new Dictionary<RectTransform, Vector3>();

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
            _gameEnergyCounter.noFail = false;
            _gameEnergyCounter._didReach0Energy = isFailingEnergy;
            _gameEnergyCounter._nextFrameEnergyChange = 0;
            _gameEnergyCounter.energy = energy;
            _gameEnergyCounter.noFail = noFail;

            PrepareEnergyUIPanel();
            FieldAccessor<GameEnergyCounter, Action<float>>.Get(_gameEnergyCounter, "gameEnergyDidChangeEvent").Invoke(energy);
            UpdateEnergyUIPanel(energy);
        }

        private void PrepareEnergyUIPanel() {

            if (_gameEnergyUIPanel != null && !_playerDataModel.playerData.playerSpecificSettings.noTextsAndHuds) {
                EnsureEnergyUIPanelReady();
                CaptureInitialEnergyIconPositions();
            }
        }

        private void UpdateEnergyUIPanel(float energy) {

            if (_gameEnergyUIPanel != null && !_playerDataModel.playerData.playerSpecificSettings.noTextsAndHuds) {
                var director = _gameEnergyUIPanel._playableDirector;
                bool directorHasOwnObject = director.gameObject != _gameEnergyUIPanel.gameObject;
                if (directorHasOwnObject)
                    director.gameObject.SetActive(true);

                director.enabled = true;
                director.Stop();
                director.time = 0f;
                director.Evaluate();
                if (directorHasOwnObject)
                    director.gameObject.SetActive(false);
                else
                    director.enabled = false;

                UpdateEnergyIcons(energy);
                RestoreInitialEnergyIconPositions();
            }
        }

        private void EnsureEnergyUIPanelReady() {

            if (_gameEnergyCounter.energyType != GameplayModifiers.EnergyType.Battery || _gameEnergyUIPanel._batteryLifeSegments != null)
                return;

            _gameEnergyUIPanel.Init();
        }

        private void UpdateEnergyIcons(float energy) {

            if (_gameEnergyCounter.energyType == GameplayModifiers.EnergyType.Battery) {
                UpdateBatteryEnergyIcons();
                return;
            }

            var energyBar = _gameEnergyUIPanel._energyBar;
            energyBar.gameObject.SetActive(true);
            energyBar.enabled = true;
            energyBar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(energy), 1f);
        }

        private void UpdateBatteryEnergyIcons() {

            var energyBar = _gameEnergyUIPanel._energyBar;
            energyBar.gameObject.SetActive(false);

            var batteryLifeSegments = _gameEnergyUIPanel._batteryLifeSegments;
            if (batteryLifeSegments == null)
                return;

            int batteryEnergy = Mathf.Clamp(_gameEnergyCounter.batteryEnergy, 0, batteryLifeSegments.Count);
            for (int i = 0; i < batteryLifeSegments.Count; i++)
                batteryLifeSegments[i].enabled = i < batteryEnergy;

            _gameEnergyUIPanel._activeBatteryLifeSegmentsCount = batteryEnergy;
        }

        private void CaptureInitialEnergyIconPositions() {

            CaptureInitialPosition(_gameEnergyUIPanel._energyBar);

            var batteryLifeSegments = _gameEnergyUIPanel._batteryLifeSegments;
            if (batteryLifeSegments == null)
                return;

            foreach (var segment in batteryLifeSegments)
                CaptureInitialPosition(segment);
        }

        private void CaptureInitialPosition(Image image) {

            if (image == null)
                return;

            var rectTransform = image.rectTransform;
            if (!_initialEnergyIconPositions.ContainsKey(rectTransform))
                _initialEnergyIconPositions[rectTransform] = rectTransform.anchoredPosition3D;
        }

        private void RestoreInitialEnergyIconPositions() {

            foreach (var initialPosition in _initialEnergyIconPositions)
                initialPosition.Key.anchoredPosition3D = initialPosition.Value;
        }
    }
}
