using IPA.Utilities.Async;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Core.Gameplay;
using ScoreSaber.Features.Replays.Format;
using ScoreSaber.Features.ScoreSubmission.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEngine;

namespace ScoreSaber.Features.Replays {
    internal class ReplayLoader {

        private readonly PlayerDataModel _playerDataModel;
        private readonly MenuTransitionsHelper _menuTransitionsHelper;
        private readonly ReplayFileCodec _replayFileCodec;
        private readonly EnvironmentsListModel _environmentsListModel;
        private readonly ReplayState _replayState;
        private readonly ScoreSubmissionService _scoreSubmissionService;
        private readonly SettingsService _settings;

        public ReplayLoader(PlayerDataModel playerDataModel, MenuTransitionsHelper menuTransitionsHelper, EnvironmentsListModel environmentsListModel, ReplayState replayState, ReplayFileCodec replayFileCodec, ScoreSubmissionService scoreSubmissionService, SettingsService settings) {

            _playerDataModel = playerDataModel;
            _menuTransitionsHelper = menuTransitionsHelper;
            _replayFileCodec = replayFileCodec;
            _environmentsListModel = environmentsListModel;
            _replayState = replayState;
            _scoreSubmissionService = scoreSubmissionService;
            _settings = settings;
        }

        public async Task Load(byte[] replay, BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, GameplayModifiers modifiers, string playerName) {
            if (replay == null || replay.Length < 4) {
                throw new ArgumentException("Replay data is empty", nameof(replay));
            }

            _replayState.BeginReplay(beatmapLevel, beatmapKey, modifiers, playerName);
            if (replay[0] == 93 && replay[1] == 0 && replay[2] == 0 && replay[3] == 128) {
                await LoadLegacyReplay(replay, beatmapLevel, beatmapKey, modifiers);
            } else {
                ReplayFile replayFile = await LoadReplay(replay);
                await StartReplay(replayFile, beatmapLevel, beatmapKey);
            }
        }

        private async Task LoadLegacyReplay(byte[] replay, BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, GameplayModifiers gameplayModifiers) {
            List<Z.Keyframe> keyframes = await Task.Run(() => {
                byte[] decompressed = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(replay);
                return AddFrames(DeserializeLegacyReplay(decompressed));
            });

            await IPA.Utilities.UnityGame.SwitchToMainThreadAsync();
            _replayState.LoadLegacyReplay(keyframes);

            PlayerData playerData = _playerDataModel.playerData;
            PlayerSpecificSettings playerSettings = playerData.playerSpecificSettings;
            if (gameplayModifiers == null) {
                gameplayModifiers = new GameplayModifiers();
            }

            _scoreSubmissionService.SuspendForReplay();
            ColorScheme colorScheme = playerData.colorSchemesSettings.GetOverrideColorScheme();
            _menuTransitionsHelper.StartStandardLevel(
                "Replay",
                beatmapKey,
                beatmapLevel,
                playerData.overrideEnvironmentSettings,
                colorScheme,
                colorScheme != null ? colorScheme.ShouldOverrideLightshowColors() : playerData.colorSchemesSettings.ShouldOverrideLightshowColors(),
                gameplayModifiers,
                playerSettings,
                null,
                _environmentsListModel,
                new GameplayAdditionalInformation("Exit Replay"),
                null,
                null,
                ReplayEnd,
                null);
        }

        private static Z.SavedData DeserializeLegacyReplay(byte[] decompressed) {
            BinaryFormatter formatter = new BinaryFormatter();
            try {
                using (var dataStream = new MemoryStream(decompressed)) {
                    return (Z.SavedData)formatter.Deserialize(dataStream);
                }
            } catch (Exception ex) {
                throw new Exception("Failed to deserialize replay!", ex);
            }
        }

        private static List<Z.Keyframe> AddFrames(Z.SavedData replayData) {
            if (replayData == null || replayData._keyframes == null) {
                return new List<Z.Keyframe>();
            }

            List<Z.Keyframe> keyframes = new List<Z.Keyframe>(replayData._keyframes.Length);
            for (int i = 0; i < replayData._keyframes.Length; i++) {
                Z.SavedData.KeyframeSerializable ks = replayData._keyframes[i];
                Z.Keyframe k = new Z.Keyframe {
                    _pos1 = new Vector3(ks._xPos1, ks._yPos1, ks._zPos1),
                    _pos2 = new Vector3(ks._xPos2, ks._yPos2, ks._zPos2),
                    _pos3 = new Vector3(ks._xPos3, ks._yPos3, ks._zPos3),
                    _rot1 = new Quaternion(ks._xRot1, ks._yRot1, ks._zRot1, ks._wRot1),
                    _rot2 = new Quaternion(ks._xRot2, ks._yRot2, ks._zRot2, ks._wRot2),
                    _rot3 = new Quaternion(ks._xRot3, ks._yRot3, ks._zRot3, ks._wRot3),
                    _time = ks._time,
                    score = ks.score,
                    combo = ks.combo
                };
                keyframes.Add(k);
            }

            return keyframes;
        }

        private Task<ReplayFile> LoadReplay(byte[] replay) => _replayFileCodec.Read(replay);

        private async Task StartReplay(ReplayFile replay, BeatmapLevel beatmapLevel, BeatmapKey beatmapKey) {
            await IPA.Utilities.UnityGame.SwitchToMainThreadAsync();
            _replayState.LoadReplay(replay);

            PlayerData playerData = _playerDataModel.playerData;
            PlayerSpecificSettings localPlayerSettings = playerData.playerSpecificSettings;
            if (_settings.Current.replayOverrideHandedness && replay.metadata.LeftHanded != localPlayerSettings.leftHanded) {
                replay.Mirror();
            }

            bool useRecordedPlayerSettings = _settings.Current.useRecordedPlayerSettings && replay.metadata.HasPlaySettingsExtension;
            PlayerSpecificSettings playerSettings = PlayerSpecificSettingsFactory.Create(localPlayerSettings,
                replay.metadata.LeftHanded,
                replay.metadata.InitialHeight,
                replay.heightKeyframes.Count > 0,
                useRecordedPlayerSettings,
                replay.metadata.NoTextsAndHuds,
                replay.metadata.SaberTrailIntensity,
                replay.metadata.HideNoteSpawnEffect,
                replay.metadata.ArcsHapticFeedback,
                replay.metadata.ArcVisibility,
                replay.metadata.EnvironmentEffectsFilterDefaultPreset,
                replay.metadata.EnvironmentEffectsFilterExpertPlusPreset);
            ColorScheme replayColorScheme = useRecordedPlayerSettings
                ? ColorSchemeFactory.Create(
                    playerData,
                    replay.metadata.LeftSaberColor,
                    replay.metadata.RightSaberColor,
                    replay.metadata.ObstacleColor,
                    replay.metadata.EnvironmentColor0,
                    replay.metadata.EnvironmentColor1,
                    replay.metadata.EnvironmentColorW,
                    replay.metadata.EnvironmentColor0Boost,
                    replay.metadata.EnvironmentColor1Boost,
                    replay.metadata.EnvironmentColorWBoost,
                    replay.metadata.SupportsEnvironmentColorBoost)
                : null;
            OverrideEnvironmentSettings replayEnvironmentSettings = OverrideEnvironmentSettingsFactory.Create(
                playerData,
                _environmentsListModel,
                replay.metadata.Environment,
                useRecordedPlayerSettings);
            ColorScheme playerColorScheme = replayColorScheme ?? playerData.colorSchemesSettings.GetOverrideColorScheme();

            _scoreSubmissionService.SuspendForReplay();
            _menuTransitionsHelper.StartStandardLevel(
                "Replay",
                beatmapKey,
                beatmapLevel,
                replayEnvironmentSettings ?? playerData.overrideEnvironmentSettings,
                playerColorScheme,
                replayColorScheme != null ? replayColorScheme.ShouldOverrideLightshowColors() : playerData.colorSchemesSettings.ShouldOverrideLightshowColors(),
                ScoreSaberGameplayModifiers.FromCodes(replay.metadata.Modifiers, false).GameplayModifiers,
                playerSettings,
                null,
                _environmentsListModel,
                new GameplayAdditionalInformation("Exit Replay"),
                null,
                null,
                ReplayEnd,
                null);
        }

        private void ReplayEnd(StandardLevelScenesTransitionSetupData standardLevelSceneSetupData, LevelCompletionResults levelCompletionResults) {

            _replayState.EndPlayback();
            _scoreSubmissionService.ResumeAfterReplay();
        }
    }
}
