using System;
using ScoreSaber.Core;
using ScoreSaber.Core.Gameplay;
using ScoreSaber.Features.Replays.Format;
using Zenject;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class MetadataRecorder : TimeSynchronizer, IInitializable, IDisposable {
        BeatmapObjectSpawnController.InitData _beatmapObjectSpawnControllerInitData;
        private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
        private readonly RoomSettings _roomSettings;
        private readonly IGameEnergyCounter _gameEnergyCounter;
        private readonly ScoreSaberRuntimeInfo _runtimeInfo;
        private readonly AudioTimeSyncController.InitData _audioTimeSyncInitData;
        private readonly GameplayMetadataProvider _metadataProvider;
        private float _failTime;

        public MetadataRecorder(GameplayCoreSceneSetupData gameplayCoreSceneSetupData, BeatmapObjectSpawnController.InitData beatmapObjectSpawnControllerInitData, IGameEnergyCounter gameEnergyCounter, RoomSettings roomSettings, ScoreSaberRuntimeInfo runtimeInfo, GameplayMetadataProvider metadataProvider, [InjectOptional] AudioTimeSyncController.InitData audioTimeSyncInitData) {

            _beatmapObjectSpawnControllerInitData = beatmapObjectSpawnControllerInitData;
            _gameEnergyCounter = gameEnergyCounter;
            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            _roomSettings = roomSettings;
            _runtimeInfo = runtimeInfo;
            _metadataProvider = metadataProvider;
            _audioTimeSyncInitData = audioTimeSyncInitData;
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
                Characteristic = _gameplayCoreSceneSetupData.GetBeatmapKey().CharacteristicSerializedName(),
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
                JumpDistance = _metadataProvider.JumpDistance(_beatmapObjectSpawnControllerInitData),
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
                ArcVisibility = _metadataProvider.ArcVisibility(_gameplayCoreSceneSetupData.playerSpecificSettings),
                ControllerOffsets = ReplayControllerOffsets(_metadataProvider.ControllerPoses()),
            };

        }

        public string[] GetModifierList(GameplayModifiers modifiers) => ScoreSaberGameplayModifiers.ToCodeList(modifiers, true).ToArray();

        private float SongSpeed() {
            if (_audioTimeSyncInitData != null && _audioTimeSyncInitData.timeScale > 0f) {
                return _audioTimeSyncInitData.timeScale;
            }

            return audioTimeSyncController != null ? audioTimeSyncController.timeScale : 1f;
        }

        private int CurrentEnvironmentEffectsFilterPreset() {
            if (_gameplayCoreSceneSetupData.GetBeatmapKey().difficulty == BeatmapDifficulty.ExpertPlus) {
                return (int)_gameplayCoreSceneSetupData.playerSpecificSettings.environmentEffectsFilterExpertPlusPreset;
            }

            return (int)_gameplayCoreSceneSetupData.playerSpecificSettings.environmentEffectsFilterDefaultPreset;
        }

        private static ReplayControllerOffsets? ReplayControllerOffsets(ControllerPoseSet? controllerPoses) {
            if (!controllerPoses.HasValue) {
                return null;
            }

            ControllerPoseSet poses = controllerPoses.Value;
            return new ReplayControllerOffsets {
                Shared = ReplayControllerOffset(poses.Shared),
                Left = ReplayControllerOffset(poses.Left),
                Right = ReplayControllerOffset(poses.Right)
            };
        }

        private static ReplayControllerOffset? ReplayControllerOffset(ControllerPose? controllerPose) {
            if (!controllerPose.HasValue) {
                return null;
            }

            ControllerPose pose = controllerPose.Value;
            return new ReplayControllerOffset {
                Position = new VRPosition { X = pose.Position.x, Y = pose.Position.y, Z = pose.Position.z },
                Rotation = new VRPosition { X = pose.Rotation.x, Y = pose.Rotation.y, Z = pose.Rotation.z }
            };
        }

    }
}
