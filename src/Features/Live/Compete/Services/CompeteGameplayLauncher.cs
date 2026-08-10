using ScoreSaber.Core.Timing;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Live.V1;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Live.Compete.Services {
    internal class CompeteGameplayLauncher {
        private const int MapStartReadyPollMs = 25;
        private const int MapStartReadyTimeoutMs = 30000;

        private readonly PlayerDataModel _playerDataModel;
        private readonly MenuTransitionsHelper _menuTransitionsHelper;
        private readonly EnvironmentsListModel _environmentsListModel;
        private readonly CompeteGameplayState _gameplayState;
        private readonly ScoreSaberClock _clock;

        internal CompeteGameplayLauncher(
            PlayerDataModel playerDataModel,
            MenuTransitionsHelper menuTransitionsHelper,
            EnvironmentsListModel environmentsListModel,
            CompeteGameplayState gameplayState,
            ScoreSaberClock clock) {

            _playerDataModel = playerDataModel;
            _menuTransitionsHelper = menuTransitionsHelper;
            _environmentsListModel = environmentsListModel;
            _gameplayState = gameplayState;
            _clock = clock;
        }

        internal async Task Start(CompeteRoom room, int delayMs, CancellationToken cancellationToken) {
            if (room == null) {
                throw new ArgumentNullException(nameof(room));
            }

            CompeteSongSelection song = room.Song;
            if (song?.BeatmapLevel == null) {
                throw new InvalidOperationException("Live room song is not installed");
            }

            if (delayMs > 0) {
                await Task.Delay(delayMs, cancellationToken);
            }

            await IPA.Utilities.UnityGame.SwitchToMainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();

            PlayerData playerData = _playerDataModel.playerData;
            GameplayModifiers modifiers = LiveGameplayModifiers();
            PlayerSpecificSettings playerSettings = playerData.playerSpecificSettings;

            _gameplayState.Begin(room.TournamentId, room.Id, song.MapHash);
            try {
                ColorScheme colorScheme = playerData.colorSchemesSettings.GetOverrideColorScheme();
                _menuTransitionsHelper.StartStandardLevel(
                    "Solo",
                    song.BeatmapKey,
                    song.BeatmapLevel,
                    playerData.overrideEnvironmentSettings,
                    colorScheme,
                    playerData.colorSchemesSettings.ShouldOverrideLightshowColors(),
                    modifiers,
                    playerSettings,
                    null,
                    _environmentsListModel,
                    new GameplayAdditionalInformation("Menu"),
                    null,
                    null,
                    LevelFinished,
                    null);
            } catch {
                _gameplayState.End();
                throw;
            }
        }

        internal async Task<bool> WaitForMapStartReady(string matchId, string mapHash, CancellationToken cancellationToken) {
            int waitedMs = 0;
            while (_gameplayState.IsCurrentMap(matchId, mapHash) && !_gameplayState.IsMapStartReady && waitedMs < MapStartReadyTimeoutMs) {
                await Task.Delay(MapStartReadyPollMs, cancellationToken);
                waitedMs += MapStartReadyPollMs;
            }

            if (!_gameplayState.IsCurrentMap(matchId, mapHash)) {
                return false;
            }

            if (!_gameplayState.IsMapStartReady) {
                Plugin.Log.Warn("Ludus: Timed out waiting for FPS start gate; sending map start presence anyway.");
                _gameplayState.MarkMapStartReady();
            }

            return true;
        }

        private void LevelFinished(StandardLevelScenesTransitionSetupData transition, LevelCompletionResults results) {
            _gameplayState.End();
        }

        private static GameplayModifiers LiveGameplayModifiers() {
            return new GameplayModifiers(
                GameplayModifiers.EnergyType.Bar,
                noFailOn0Energy: true,
                instaFail: false,
                failOnSaberClash: false,
                enabledObstacleType: GameplayModifiers.EnabledObstacleType.All,
                noBombs: false,
                fastNotes: false,
                strictAngles: false,
                disappearingArrows: false,
                songSpeed: GameplayModifiers.SongSpeed.Normal,
                noArrows: false,
                ghostNotes: false,
                proMode: false,
                zenMode: false,
                smallCubes: false);
        }

        internal int StartDelayMs(ServerCommand command) {
            if (command == null) {
                return 0;
            }

            long now = _clock.UnixTimeMilliseconds();
            if (command.StartTimeUnixMs > now) {
                return ClampDelay(command.StartTimeUnixMs - now);
            }

            return ClampDelay(command.CountdownMs);
        }

        private static int ClampDelay(long delayMs) {
            if (delayMs <= 0) {
                return 0;
            }

            return delayMs > int.MaxValue ? int.MaxValue : (int)delayMs;
        }
    }
}
