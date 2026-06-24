using System;
#if BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
using BeatSaber.GameSettings;
#endif
using ScoreSaber.Core;
using ScoreSaber.Core.Compat;
using ScoreSaber.Core.Gameplay;
using ScoreSaber.Features.Replays.Format;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class MetadataRecorder : TimeSynchronizer, IInitializable, IDisposable {
        BeatmapObjectSpawnController.InitData _beatmapObjectSpawnControllerInitData;
        private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
        private readonly RoomSettings _roomSettings;
        private readonly IGameEnergyCounter _gameEnergyCounter;
        private readonly ScoreSaberRuntimeInfo _runtimeInfo;
        private readonly AudioTimeSyncController.InitData _audioTimeSyncInitData;
#if BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
        private readonly VariableMovementDataProvider _movementDataProvider;
        private readonly ControllerProfilesModel _controllerProfilesModel;
#elif BEAT_SABER_1_38_0
        private readonly SettingsManager _settingsManager;
#endif
        private float _failTime;

        public MetadataRecorder(GameplayCoreSceneSetupData gameplayCoreSceneSetupData, BeatmapObjectSpawnController.InitData beatmapObjectSpawnControllerInitData, IGameEnergyCounter gameEnergyCounter, RoomSettings roomSettings, ScoreSaberRuntimeInfo runtimeInfo, [InjectOptional] AudioTimeSyncController.InitData audioTimeSyncInitData
#if BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            , [InjectOptional] VariableMovementDataProvider movementDataProvider, [InjectOptional] ControllerProfilesModel controllerProfilesModel
#elif BEAT_SABER_1_38_0
            , [InjectOptional] SettingsManager settingsManager
#endif
            ) {

            _beatmapObjectSpawnControllerInitData = beatmapObjectSpawnControllerInitData;
            _gameEnergyCounter = gameEnergyCounter;
            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            _roomSettings = roomSettings;
            _runtimeInfo = runtimeInfo;
            _audioTimeSyncInitData = audioTimeSyncInitData;
#if BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            _movementDataProvider = movementDataProvider;
            _controllerProfilesModel = controllerProfilesModel;
#elif BEAT_SABER_1_38_0
            _settingsManager = settingsManager;
#endif
        }

        public void Initialize() => _gameEnergyCounter.gameEnergyDidReach0Event += GameEnergyCounter_gameEnergyDidReach0Event;

        public void Dispose() => _gameEnergyCounter.gameEnergyDidReach0Event -= GameEnergyCounter_gameEnergyDidReach0Event;

        private void GameEnergyCounter_gameEnergyDidReach0Event() => _failTime = audioTimeSyncController.songTime;

        public Metadata Export() {

            VRPosition roomCenter = new VRPosition() {
                X = _roomSettings.Center.x,
                Y = _roomSettings.Center.y,
                Z = _roomSettings.Center.z
            };

            return new Metadata() {
                Version = new Version("3.1.0"),
                LevelID = _gameplayCoreSceneSetupData.GetBeatmapLevel().levelID,
                Difficulty = BeatmapDifficultyMethods.DefaultRating(_gameplayCoreSceneSetupData.GetBeatmapKey().difficulty),
                Characteristic = _gameplayCoreSceneSetupData.GetBeatmapKey().beatmapCharacteristic.serializedName,
                Environment = _gameplayCoreSceneSetupData.GetEnvironmentSerializedName(),
                Modifiers = GetModifierList(_gameplayCoreSceneSetupData.gameplayModifiers),
                NoteSpawnOffset = _beatmapObjectSpawnControllerInitData.noteJumpValue,
                LeftHanded = _gameplayCoreSceneSetupData.playerSpecificSettings.leftHanded,
                InitialHeight = _gameplayCoreSceneSetupData.playerSpecificSettings.playerHeight,
                RoomRotation = _roomSettings.Rotation,
                RoomCenter = roomCenter,
                FailTime = _failTime,
                GameVersion = _runtimeInfo.GameVersion,
                PluginVersion = _runtimeInfo.PluginVersion,
                Platform = "PC",
                HasPlaySettingsExtension = true,
                SongSpeed = SongSpeed(),
                JumpDistance = JumpDistance(),
                LeftSaberColor = _gameplayCoreSceneSetupData.colorScheme?.saberAColor,
                RightSaberColor = _gameplayCoreSceneSetupData.colorScheme?.saberBColor,
                ObstacleColor = _gameplayCoreSceneSetupData.colorScheme?.obstaclesColor,
                EnvironmentColor0 = _gameplayCoreSceneSetupData.colorScheme?.environmentColor0,
                EnvironmentColor1 = _gameplayCoreSceneSetupData.colorScheme?.environmentColor1,
                EnvironmentColorW = _gameplayCoreSceneSetupData.colorScheme?.environmentColorW,
                EnvironmentColor0Boost = _gameplayCoreSceneSetupData.colorScheme?.environmentColor0Boost,
                EnvironmentColor1Boost = _gameplayCoreSceneSetupData.colorScheme?.environmentColor1Boost,
                EnvironmentColorWBoost = _gameplayCoreSceneSetupData.colorScheme?.environmentColorWBoost,
                SupportsEnvironmentColorBoost = _gameplayCoreSceneSetupData.colorScheme?.supportsEnvironmentColorBoost ?? false,
                EnvironmentEffectsFilterDefaultPreset = (int)_gameplayCoreSceneSetupData.playerSpecificSettings.environmentEffectsFilterDefaultPreset,
                EnvironmentEffectsFilterExpertPlusPreset = (int)_gameplayCoreSceneSetupData.playerSpecificSettings.environmentEffectsFilterExpertPlusPreset,
                EnvironmentEffectsFilterPreset = CurrentEnvironmentEffectsFilterPreset(),
                NoTextsAndHuds = _gameplayCoreSceneSetupData.playerSpecificSettings.noTextsAndHuds,
                SaberTrailIntensity = _gameplayCoreSceneSetupData.playerSpecificSettings.saberTrailIntensity,
                HideNoteSpawnEffect = _gameplayCoreSceneSetupData.playerSpecificSettings.hideNoteSpawnEffect,
                ArcsHapticFeedback = _gameplayCoreSceneSetupData.playerSpecificSettings.arcsHapticFeedback,
                ArcVisibility = ArcVisibility(),
                ControllerOffsets = ControllerOffsets(),
            };

        }

        public string[] GetModifierList(GameplayModifiers modifiers) => ScoreSaberGameplayModifiers.ToCodeList(modifiers, true).ToArray();

        private float SongSpeed() {
            if (_audioTimeSyncInitData != null && _audioTimeSyncInitData.timeScale > 0f) {
                return _audioTimeSyncInitData.timeScale;
            }

            return audioTimeSyncController != null ? audioTimeSyncController.timeScale : 1f;
        }

        private float JumpDistance() {
#if BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            if (_movementDataProvider != null && _movementDataProvider.jumpDistance > 0f) {
                return _movementDataProvider.jumpDistance;
            }
#endif

            if (_beatmapObjectSpawnControllerInitData.noteJumpValueType == BeatmapObjectSpawnMovementData.NoteJumpValueType.JumpDuration) {
                return _beatmapObjectSpawnControllerInitData.noteJumpMovementSpeed * _beatmapObjectSpawnControllerInitData.noteJumpValue * 2f;
            }

            if (_beatmapObjectSpawnControllerInitData.beatsPerMinute <= 0f) {
                return 0f;
            }

            float halfJumpDuration = 4f;
            float beatDuration = 60f / _beatmapObjectSpawnControllerInitData.beatsPerMinute;
            while (_beatmapObjectSpawnControllerInitData.noteJumpMovementSpeed * beatDuration * halfJumpDuration > 17.999f) {
                halfJumpDuration /= 2f;
            }

            halfJumpDuration += _beatmapObjectSpawnControllerInitData.noteJumpValue;
            if (halfJumpDuration < 0.25f) {
                halfJumpDuration = 0.25f;
            }

            return _beatmapObjectSpawnControllerInitData.noteJumpMovementSpeed * beatDuration * halfJumpDuration * 2f;
        }

        private int CurrentEnvironmentEffectsFilterPreset() {
            if (_gameplayCoreSceneSetupData.GetBeatmapKey().difficulty == BeatmapDifficulty.ExpertPlus) {
                return (int)_gameplayCoreSceneSetupData.playerSpecificSettings.environmentEffectsFilterExpertPlusPreset;
            }

            return (int)_gameplayCoreSceneSetupData.playerSpecificSettings.environmentEffectsFilterDefaultPreset;
        }

        private int ArcVisibility() {
#if BEAT_SABER_1_29_0
            return (int)_gameplayCoreSceneSetupData.playerSpecificSettings.arcsVisible;
#else
            return (int)_gameplayCoreSceneSetupData.playerSpecificSettings.arcVisibility;
#endif
        }

        private ReplayControllerOffsets? ControllerOffsets() {
#if BEAT_SABER_1_29_0
            MainSettingsModelSO[] settings = Resources.FindObjectsOfTypeAll<MainSettingsModelSO>();
            if (settings.Length == 0) {
                return null;
            }

            return new ReplayControllerOffsets() {
                Shared = ControllerOffset(settings[0].controllerPosition.value, settings[0].controllerRotation.value)
            };
#elif BEAT_SABER_1_37_1
            return null;
#elif BEAT_SABER_1_38_0
            if (_settingsManager == null) {
                return null;
            }

            var controller = _settingsManager.settings.controller;
            return new ReplayControllerOffsets() {
                Shared = ControllerOffset(
                    new Vector3(controller.position.x, controller.position.y, controller.position.z),
                    new Vector3(controller.rotation.x, controller.rotation.y, controller.rotation.z))
            };
#elif BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            if (_controllerProfilesModel == null) {
                return null;
            }

            var profile = _controllerProfilesModel.selectedProfile;
            return new ReplayControllerOffsets() {
                Left = ControllerOffset(profile.leftController.position, profile.leftController.rotation),
                Right = ControllerOffset(profile.rightController.position, profile.rightController.rotation)
            };
#endif
        }

        private static ReplayControllerOffset ControllerOffset(Vector3 position, Vector3 rotation) {

            return new ReplayControllerOffset() {
                Position = new VRPosition() {
                    X = position.x,
                    Y = position.y,
                    Z = position.z
                },
                Rotation = new VRPosition() {
                    X = rotation.x,
                    Y = rotation.y,
                    Z = rotation.z
                }
            };
        }

    }
}
