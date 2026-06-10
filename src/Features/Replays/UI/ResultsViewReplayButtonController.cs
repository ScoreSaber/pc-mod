using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using IPA.Utilities.Async;
using ScoreSaber.Features.Players.Services;
using ScoreSaber.Core;
using ScoreSaber.Core.Compat;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ScoreSaber.Features.Replays.UI {
    internal class ResultsViewReplayButtonController : IInitializable, IDisposable {
        [UIComponent("watch-replay-button")]
        protected readonly Button watchReplayButton = null;

        private ResultsViewController _resultsViewController;
        private BeatmapLevel _beatmapLevel;
        private BeatmapKey _beatmapKey;
        private LevelCompletionResults _levelCompletionResults;
        private readonly GameSessionService _gameSessionService;
        private readonly ReplayLoader _replayLoader;
        private readonly ReplayService _replayService;

        private byte[] _serializedReplay;
        private int _waitForReplayVersion;

        public ResultsViewReplayButtonController(ResultsViewController resultsViewController, GameSessionService gameSessionService, ReplayLoader replayLoader, ReplayService replayService) {

            _resultsViewController = resultsViewController;
            _gameSessionService = gameSessionService;
            _replayLoader = replayLoader;
            _replayService = replayService;
        }

        public void Initialize() {

            _resultsViewController.didActivateEvent += ResultsViewController_didActivateEvent;
            _resultsViewController.continueButtonPressedEvent += ResultsViewController_continueButtonPressedEvent;
            _resultsViewController.restartButtonPressedEvent += ResultsViewController_restartButtonPressedEvent;
            _replayService.ReplaySerialized += ReplayServiceReplaySerialized;
        }

        private void ResultsViewController_didActivateEvent(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) {

            if (firstActivation) {
                BsmlCompat.Parser.Parse(
                    "<button-with-icon id=\"watch-replay-button\" icon=\"ScoreSaber.Resources.replay.png\" hover-hint=\"Watch Replay\" pref-width=\"15\" pref-height=\"13\" interactable=\"false\" on-click=\"replay-click\" />",
                    _resultsViewController.gameObject,
                    this
                );
                watchReplayButton.transform.localScale *= 0.4f;
                watchReplayButton.transform.localPosition = new Vector2(42.5f, 27f);
            }
            watchReplayButton.interactable = _serializedReplay != null;
            _beatmapLevel = _resultsViewController.GetBeatmapLevel();
            _beatmapKey = _resultsViewController.GetBeatmapKey();
            _levelCompletionResults = _resultsViewController._levelCompletionResults;
            WaitForReplay(++_waitForReplayVersion).RunTask();
        }

        private void ResultsViewController_restartButtonPressedEvent(ResultsViewController obj) {

            _serializedReplay = null;
            _waitForReplayVersion++;
        }

        private void ResultsViewController_continueButtonPressedEvent(ResultsViewController obj) {

            _serializedReplay = null;
            _waitForReplayVersion++;
        }

        private void ReplayServiceReplaySerialized(byte[] replay) {

            _serializedReplay = replay;
        }

        private async Task WaitForReplay(int version) {

            await ScoreSaber.Core.TaskExtensions.WaitUntil(() => _serializedReplay != null || version != _waitForReplayVersion);
            if (version == _waitForReplayVersion && _serializedReplay != null) {
                watchReplayButton.interactable = true;
            }
        }

        public void Dispose() {

            _waitForReplayVersion++;
            _resultsViewController.didActivateEvent -= ResultsViewController_didActivateEvent;
            _resultsViewController.continueButtonPressedEvent -= ResultsViewController_continueButtonPressedEvent;
            _resultsViewController.restartButtonPressedEvent -= ResultsViewController_restartButtonPressedEvent;
            _replayService.ReplaySerialized -= ReplayServiceReplaySerialized;
        }

        [UIAction("replay-click")]
        protected void ClickedReplayButton() {

            _replayLoader.Load(_serializedReplay, _beatmapLevel, _beatmapKey, _levelCompletionResults.gameplayModifiers, _gameSessionService.LocalPlayerInfo.playerName).RunTask();
            watchReplayButton.interactable = false;

        }
    }
}
