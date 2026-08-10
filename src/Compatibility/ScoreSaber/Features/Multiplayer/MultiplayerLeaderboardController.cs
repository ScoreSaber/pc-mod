using HMUI;
using IPA.Loader;
using IPA.Utilities;
using LeaderboardCore.Models;
using System;
using Zenject;
using HiveVersion = Hive.Versioning.Version;

namespace ScoreSaber.Features.Multiplayer {
    internal class MultiplayerLeaderboardController : IInitializable, IDisposable {
        private const string LegacyScoreSaberLeaderboardCoreType = "LeaderboardCore.Models.ScoreSaberCustomLeaderboard";
        private static readonly HiveVersion MaxHandledLeaderboardCoreVersion = new HiveVersion("1.7.0");

        private readonly MainFlowCoordinator _mainFlowCoordinator;
        private readonly MultiplayerLobbyController _multiplayerLobbyController;
        private readonly ServerPlayerListViewController _serverPlayerListViewController;
        private readonly PlatformLeaderboardViewController _platformLeaderboardViewController;
        private readonly LevelSelectionNavigationController _levelSelectionNavigationController;
        private bool _subscribed;

        public MultiplayerLeaderboardController(
            MainFlowCoordinator mainFlowCoordinator,
            MultiplayerLobbyController multiplayerLobbyController,
            ServerPlayerListViewController serverPlayerListViewController,
            PlatformLeaderboardViewController platformLeaderboardViewController,
            LevelSelectionNavigationController levelSelectionNavigationController) {
            _mainFlowCoordinator = mainFlowCoordinator;
            _multiplayerLobbyController = multiplayerLobbyController;
            _serverPlayerListViewController = serverPlayerListViewController;
            _platformLeaderboardViewController = platformLeaderboardViewController;
            _levelSelectionNavigationController = levelSelectionNavigationController;
        }

        public void Initialize() {
            if (!ShouldHandleMultiplayerLeaderboards()) {
                return;
            }

            _levelSelectionNavigationController.didChangeDifficultyBeatmapEvent += LevelSelectionNavigationControllerDidChangeDifficultyBeatmapEvent;
            _levelSelectionNavigationController.didChangeLevelDetailContentEvent += LevelSelectionNavigationControllerDidChangeLevelDetailContentEvent;
            _subscribed = true;
        }

        public void Dispose() {
            if (!_subscribed) {
                return;
            }

            _levelSelectionNavigationController.didChangeDifficultyBeatmapEvent -= LevelSelectionNavigationControllerDidChangeDifficultyBeatmapEvent;
            _levelSelectionNavigationController.didChangeLevelDetailContentEvent -= LevelSelectionNavigationControllerDidChangeLevelDetailContentEvent;
        }

        private static bool ShouldHandleMultiplayerLeaderboards() {
            PluginMetadata metadata = PluginManager.GetPluginFromId("LeaderboardCore");
            return metadata != null
                && metadata.HVersion.CompareTo(MaxHandledLeaderboardCoreVersion) <= 0
                && typeof(CustomLeaderboard).Assembly.GetType(LegacyScoreSaberLeaderboardCoreType) != null;
        }

        private bool CanShowLeaderboard(bool hasSelectedBeatmap) {
            if (!_multiplayerLobbyController.lobbyActivated) {
                return false;
            }

            if (!hasSelectedBeatmap) {
                HideLeaderboard();
                return false;
            }

            return true;
        }

#if BEAT_SABER_1_29_0
        private void ShowLeaderboard(IDifficultyBeatmap difficultyBeatmap) {
            if (!CanShowLeaderboard(difficultyBeatmap != null)) {
                return;
            }

            _platformLeaderboardViewController.SetData(new BeatmapKey(difficultyBeatmap));
            ShowLeaderboardView();
        }
#else
        private void ShowLeaderboard(BeatmapLevel beatmapLevel) {
            if (!CanShowLeaderboard(beatmapLevel != null)) {
                return;
            }

            _platformLeaderboardViewController.SetData(_levelSelectionNavigationController.GetBeatmapKey());
            ShowLeaderboardView();
        }
#endif

        private void ShowLeaderboardView() {
            _mainFlowCoordinator
                .YoungestChildFlowCoordinatorOrSelf()
                .InvokeMethod<object, FlowCoordinator>("SetRightScreenViewController", _platformLeaderboardViewController, ViewController.AnimationType.In);
            _serverPlayerListViewController.DeactivateGameObject();
        }

        private void HideLeaderboard() {
            _mainFlowCoordinator
                .YoungestChildFlowCoordinatorOrSelf()
                .InvokeMethod<object, FlowCoordinator>("SetRightScreenViewController", null, ViewController.AnimationType.Out);
        }

        private void LevelSelectionNavigationControllerDidChangeLevelDetailContentEvent(LevelSelectionNavigationController levelSelectionNavigationController, StandardLevelDetailViewController.ContentType contentType) {
            if (contentType == StandardLevelDetailViewController.ContentType.OwnedAndReady) {
#if BEAT_SABER_1_29_0
                ShowLeaderboard(levelSelectionNavigationController.selectedDifficultyBeatmap);
#else
                ShowLeaderboard(levelSelectionNavigationController.beatmapLevel);
#endif
                return;
            }

            ShowLeaderboard(null);
        }

#if BEAT_SABER_1_29_0
        private void LevelSelectionNavigationControllerDidChangeDifficultyBeatmapEvent(LevelSelectionNavigationController levelSelectionNavigationController, IDifficultyBeatmap difficultyBeatmap) {
            ShowLeaderboard(difficultyBeatmap);
        }
#else
        private void LevelSelectionNavigationControllerDidChangeDifficultyBeatmapEvent(LevelSelectionNavigationController levelSelectionNavigationController) {
            ShowLeaderboard(levelSelectionNavigationController.beatmapLevel);
        }
#endif
    }
}
