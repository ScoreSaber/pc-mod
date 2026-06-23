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
        private readonly AudioTimeSyncController.InitData _audioTimeSyncInitData;
        private float _failTime;

        public MetadataRecorder(GameplayCoreSceneSetupData gameplayCoreSceneSetupData, BeatmapObjectSpawnController.InitData beatmapObjectSpawnControllerInitData, IGameEnergyCounter gameEnergyCounter, RoomSettings roomSettings, ScoreSaberRuntimeInfo runtimeInfo, [InjectOptional] AudioTimeSyncController.InitData audioTimeSyncInitData) {

            _beatmapObjectSpawnControllerInitData = beatmapObjectSpawnControllerInitData;
            _gameEnergyCounter = gameEnergyCounter;
            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            _roomSettings = roomSettings;
            _runtimeInfo = runtimeInfo;
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
                SongSpeed = SongSpeed(),
                JumpDistance = JumpDistance(),
                LeftSaberColor = _gameplayCoreSceneSetupData.colorScheme?.saberAColor,
                RightSaberColor = _gameplayCoreSceneSetupData.colorScheme?.saberBColor,
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

    }
}
