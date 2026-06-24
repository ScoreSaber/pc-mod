using HMUI;
using IPA.Utilities;
using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ScoreSaber.Features.Replays.Playback {
    internal class EnergyPlayer : TimeSynchronizer, IScroller {
        private const float EnergyIconPositionX = 59f;
        private const string LaserCloudName = "Laser";
        private const string EnergyIconEmptyName = "EnergyIconEmpty";
        private const string EnergyIconFullName = "EnergyIconFull";
        private const float EnergyIconTransparentAlpha = 0.251f;

        private GameEnergyCounter _gameEnergyCounter;
        private GameEnergyUIPanel _gameEnergyUIPanel;
        private PlayerDataModel _playerDataModel;
        private readonly List<EnergyEvent> _energyEvents;
        private readonly Dictionary<RectTransform, Vector3> _initialEnergyIconPositions = new Dictionary<RectTransform, Vector3>();
        private bool _energyUIPanelObjectsResolved;
        private Transform _laserCloud;
        private Transform _energyIconFull;
        private Transform _energyIconEmpty;
        private ImageView _energyIconFullImage;
        private ImageView _energyIconEmptyImage;

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
                RestoreEnergyBarIconPositions(energy);
            }
        }

        private void EnsureEnergyUIPanelReady() {

            if (_gameEnergyCounter.energyType != GameplayModifiers.EnergyType.Battery || _gameEnergyUIPanel._batteryLifeSegments != null)
                return;

            _gameEnergyUIPanel.Init();
        }

        private void UpdateEnergyIcons(float energy) {

            ResolveEnergyUIPanelObjects();

            if (energy >= Mathf.Epsilon) {
                _laserCloud?.gameObject.SetActive(false);
            }
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

        private void RestoreEnergyBarIconPositions(float energy) {

            if (energy <= Mathf.Epsilon)
                return;

            ResolveEnergyUIPanelObjects();

            if (_energyIconFull != null) {
                _energyIconFull.localPosition = new Vector3(EnergyIconPositionX, 0f, _energyIconFull.localPosition.z);
                SetEnergyIconAlpha(_energyIconFullImage);
            }

            if (_energyIconEmpty != null) {
                _energyIconEmpty.localPosition = new Vector3(-EnergyIconPositionX, 0f, _energyIconEmpty.localPosition.z);
                SetEnergyIconAlpha(_energyIconEmptyImage);
            }
        }

        private void ResolveEnergyUIPanelObjects() {

            if (_energyUIPanelObjectsResolved || _gameEnergyUIPanel == null)
                return;

            foreach (Transform transform in _gameEnergyUIPanel.GetComponentsInChildren<Transform>(true)) {
                if (transform.name == LaserCloudName) {
                    _laserCloud = transform;
                } else if (transform.name == EnergyIconFullName) {
                    _energyIconFull = transform;
                    _energyIconFullImage = transform.GetComponent<ImageView>();
                } else if (transform.name == EnergyIconEmptyName) {
                    _energyIconEmpty = transform;
                    _energyIconEmptyImage = transform.GetComponent<ImageView>();
                }
            }

            _energyUIPanelObjectsResolved = true;
        }

        private static void SetEnergyIconAlpha(ImageView image) {

            if (image == null)
                return;

            Color color = image.color;
            color.a = EnergyIconTransparentAlpha;
            image.color = color;
        }
    }
}
