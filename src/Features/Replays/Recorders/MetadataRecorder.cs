using System;
using ScoreSaber.Core;
using ScoreSaber.Core.Compat;
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
        private float _failTime;

        public MetadataRecorder(GameplayCoreSceneSetupData gameplayCoreSceneSetupData, BeatmapObjectSpawnController.InitData beatmapObjectSpawnControllerInitData, IGameEnergyCounter gameEnergyCounter, RoomSettings roomSettings, ScoreSaberRuntimeInfo runtimeInfo) {

            _beatmapObjectSpawnControllerInitData = beatmapObjectSpawnControllerInitData;
            _gameEnergyCounter = gameEnergyCounter;
            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            _roomSettings = roomSettings;
            _runtimeInfo = runtimeInfo;
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
            };

        }

        public string[] GetModifierList(GameplayModifiers modifiers) => ScoreSaberGameplayModifiers.ToCodeList(modifiers, true).ToArray();

    }
}
