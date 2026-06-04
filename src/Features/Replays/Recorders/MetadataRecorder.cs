using System;
using ScoreSaber.Core;
using ScoreSaber.Core.Gameplay;
using ScoreSaber.Features.Replays.Format;
using Zenject;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class MetadataRecorder : TimeSynchronizer, IInitializable, IDisposable {
        BeatmapObjectSpawnController.InitData _beatmapObjectSpawnControllerInitData;
        private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
        private readonly SettingsManager _settingsManager;
        private readonly IGameEnergyCounter _gameEnergyCounter;
        private readonly ScoreSaberRuntimeInfo _runtimeInfo;
        private float _failTime;

        public MetadataRecorder(GameplayCoreSceneSetupData gameplayCoreSceneSetupData, BeatmapObjectSpawnController.InitData beatmapObjectSpawnControllerInitData, IGameEnergyCounter gameEnergyCounter, SettingsManager settingsManager, ScoreSaberRuntimeInfo runtimeInfo) {

            _beatmapObjectSpawnControllerInitData = beatmapObjectSpawnControllerInitData;
            _gameEnergyCounter = gameEnergyCounter;
            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            _settingsManager = settingsManager;
            _runtimeInfo = runtimeInfo;
        }

        public void Initialize() => _gameEnergyCounter.gameEnergyDidReach0Event += GameEnergyCounter_gameEnergyDidReach0Event;

        public void Dispose() => _gameEnergyCounter.gameEnergyDidReach0Event -= GameEnergyCounter_gameEnergyDidReach0Event;

        private void GameEnergyCounter_gameEnergyDidReach0Event() => _failTime = audioTimeSyncController.songTime;

        public Metadata Export() {

            VRPosition roomCenter = new VRPosition() {
                X = _settingsManager.settings.room.center.x,
                Y = _settingsManager.settings.room.center.y,
                Z = _settingsManager.settings.room.center.z
            };

            return new Metadata() {
                Version = new Version("3.1.0"),
                LevelID = _gameplayCoreSceneSetupData.beatmapLevel.levelID,
                Difficulty = BeatmapDifficultyMethods.DefaultRating(_gameplayCoreSceneSetupData.beatmapKey.difficulty),
                Characteristic = _gameplayCoreSceneSetupData.beatmapKey.beatmapCharacteristic.serializedName,
                Environment = _gameplayCoreSceneSetupData.targetEnvironmentInfo.serializedName,
                Modifiers = GetModifierList(_gameplayCoreSceneSetupData.gameplayModifiers),
                NoteSpawnOffset = _beatmapObjectSpawnControllerInitData.noteJumpValue,
                LeftHanded = _gameplayCoreSceneSetupData.playerSpecificSettings.leftHanded,
                InitialHeight = _gameplayCoreSceneSetupData.playerSpecificSettings.playerHeight,
                RoomRotation = _settingsManager.settings.room.rotation,
                RoomCenter = roomCenter,
                FailTime = _failTime,
                GameVersion = _runtimeInfo.GameVersion,
                PluginVersion = _runtimeInfo.PluginVersion,
                Platform = "PC",
            };

        }

        public string[] GetModifierList(GameplayModifiers modifiers) => ScoreSaberGameplayModifiers.ToCodeList(modifiers, true).ToArray();

    }
}
