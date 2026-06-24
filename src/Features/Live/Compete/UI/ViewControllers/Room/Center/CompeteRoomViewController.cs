using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.UI.Components;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScoreSaber.Features.Live.Compete.UI.ViewControllers.Room.Center {
    [HotReload]
    internal class CompeteRoomViewController : BSMLAutomaticViewController {
        internal event Action ReadyToggled;
        internal event Action PlayersPanelSelected;
        internal event Action LeaderboardPanelSelected;
        internal event Action<CompeteOrganizerPrompt, bool> PromptAnswered;

        [UIParams]
        private readonly BSMLParserParams _parserParams = null;

        [UIComponent("song-cover")]
        private readonly ImageView _songCoverImage = null;

        [UIComponent("song-text-column")]
        private readonly LayoutElement _songTextColumnLayout = null;

        [UIComponent("song-name-text")]
        private readonly TextMeshProUGUI _songNameText = null;

        [UIComponent("song-detail-text")]
        private readonly TextMeshProUGUI _songDetailText = null;

        [UIComponent("song-difficulty-text")]
        private readonly TextMeshProUGUI _songDifficultyText = null;

        [UIComponent("song-content")]
        private readonly RectTransform _songContentTransform = null;

        private readonly CompeteSongPreview _songPreview = new CompeteSongPreview();
        private CompeteOrganizerPrompt _activePrompt;
        private string _roomTitle = "Room";
        private string _songStatus = string.Empty;
        private string _readyText = "Ready";
        private string _promptMessage = string.Empty;
        private string _promptPrimary = "Confirm";
        private string _promptSecondary = "Dismiss";
        private string _mapStartCountdownNumber = "0";
        private string _mapStartCountdownDetail = "seconds";

        [UIValue("room-title")]
        private string roomTitle {
            get => _roomTitle;
            set => SetValue(ref _roomTitle, value, nameof(roomTitle));
        }

        [UIValue("song-name")]
        private string songName => _songPreview.Name;

        [UIValue("song-detail")]
        private string songDetail => _songPreview.Detail;

        [UIValue("song-difficulty")]
        private string songDifficulty => _songPreview.Difficulty;

        [UIValue("song-duration")]
        private string songDuration => _songPreview.Duration;

        [UIValue("song-bpm")]
        private string songBpm => _songPreview.Bpm;

        [UIValue("song-nps")]
        private string songNps => _songPreview.Nps;

        [UIValue("song-notes")]
        private string songNotes => _songPreview.Notes;

        [UIValue("song-obstacles")]
        private string songObstacles => _songPreview.Obstacles;

        [UIValue("song-bombs")]
        private string songBombs => _songPreview.Bombs;

        [UIValue("song-njs")]
        private string songNjs => _songPreview.Njs;

        [UIValue("song-jump-distance")]
        private string songJumpDistance => _songPreview.JumpDistance;

        [UIValue("song-stars")]
        private string songStars => _songPreview.Stars;

        [UIValue("song-status")]
        private string songStatus {
            get => _songStatus;
            set => SetValue(ref _songStatus, value, nameof(songStatus));
        }

        [UIValue("ready-text")]
        private string readyText {
            get => _readyText;
            set => SetValue(ref _readyText, value, nameof(readyText));
        }

        [UIValue("prompt-message")]
        private string promptMessage {
            get => _promptMessage;
            set => SetValue(ref _promptMessage, value, nameof(promptMessage));
        }

        [UIValue("prompt-primary")]
        private string promptPrimary {
            get => _promptPrimary;
            set => SetValue(ref _promptPrimary, value, nameof(promptPrimary));
        }

        [UIValue("prompt-secondary")]
        private string promptSecondary {
            get => _promptSecondary;
            set => SetValue(ref _promptSecondary, value, nameof(promptSecondary));
        }

        [UIValue("map-start-countdown-number")]
        private string mapStartCountdownNumber {
            get => _mapStartCountdownNumber;
            set => SetValue(ref _mapStartCountdownNumber, value, nameof(mapStartCountdownNumber));
        }

        [UIValue("map-start-countdown-detail")]
        private string mapStartCountdownDetail {
            get => _mapStartCountdownDetail;
            set => SetValue(ref _mapStartCountdownDetail, value, nameof(mapStartCountdownDetail));
        }

        [UIValue("song-empty")]
        private bool songEmpty => _songPreview.IsEmpty && !songStatusActive;

        [UIValue("song-active")]
        private bool songActive => _songPreview.IsActive && !songStatusActive;

        [UIValue("song-status-active")]
        private bool songStatusActive => !string.IsNullOrEmpty(_songStatus);

        internal bool ReadyForPrompt => _parserParams != null && isInViewControllerHierarchy;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            BindSongPreview();
            _songPreview.RefreshVisuals();
        }

        [UIAction("#post-parse")]
        private void Parsed() {
            BindSongPreview();
            _songPreview.RefreshVisuals();
        }

        internal void SetRoom(CompeteRoom room) {
            roomTitle = room.DisplayName;
            songStatus = room.SongStatus;
            readyText = room.LocalPlayerReady ? "Not Ready" : "Ready";
            _songPreview.SetSong(room.Song);
            NotifySongPreviewChanged();
            _songPreview.RefreshVisuals();
        }

        internal void ShowPrompt(CompeteOrganizerPrompt prompt) {
            _activePrompt = prompt;
            promptMessage = prompt.Message;
            promptPrimary = prompt.PrimaryText;
            promptSecondary = prompt.SecondaryText;
            _parserParams.EmitEvent("show-organiser-prompt");
        }

        internal void ClearPrompt() {
            _activePrompt = null;
            _parserParams?.EmitEvent("hide-organiser-prompt");
        }

        internal void ShowMapStartCountdown(CompeteMapStartCountdown countdown) {
            mapStartCountdownNumber = Math.Max(0, countdown.RemainingSeconds).ToString();
            mapStartCountdownDetail = CountdownDetail(countdown.RemainingSeconds);
            _parserParams?.EmitEvent("show-map-start-countdown");
        }

        internal void HideMapStartCountdown() {
            _parserParams?.EmitEvent("hide-map-start-countdown");
        }

        [UIAction("toggle-ready")]
        private void ToggleReadyClicked() {
            ReadyToggled?.Invoke();
        }

        [UIAction("show-players")]
        private void ShowPlayersClicked() {
            PlayersPanelSelected?.Invoke();
        }

        [UIAction("show-leaderboard")]
        private void ShowLeaderboardClicked() {
            LeaderboardPanelSelected?.Invoke();
        }

        [UIAction("prompt-primary-clicked")]
        private void PromptPrimaryClicked() {
            AnswerPrompt(true);
        }

        [UIAction("prompt-secondary-clicked")]
        private void PromptSecondaryClicked() {
            AnswerPrompt(false);
        }

        private void AnswerPrompt(bool accepted) {
            _parserParams.EmitEvent("hide-organiser-prompt");
            PromptAnswered?.Invoke(_activePrompt, accepted);
            _activePrompt = null;
        }

        private static string CountdownDetail(int remainingSeconds) {
            if (remainingSeconds <= 0) {
                return "Starting...";
            }

            return remainingSeconds == 1 ? "second" : "seconds";
        }

        private void BindSongPreview() {
            _songPreview.Bind(
                _songCoverImage,
                _songTextColumnLayout,
                _songNameText,
                _songDetailText,
                _songDifficultyText,
                _songContentTransform);
        }

        private void NotifySongPreviewChanged() {
            NotifyPropertyChanged(nameof(songName));
            NotifyPropertyChanged(nameof(songDetail));
            NotifyPropertyChanged(nameof(songDifficulty));
            NotifyPropertyChanged(nameof(songDuration));
            NotifyPropertyChanged(nameof(songBpm));
            NotifyPropertyChanged(nameof(songNps));
            NotifyPropertyChanged(nameof(songNotes));
            NotifyPropertyChanged(nameof(songObstacles));
            NotifyPropertyChanged(nameof(songBombs));
            NotifyPropertyChanged(nameof(songNjs));
            NotifyPropertyChanged(nameof(songJumpDistance));
            NotifyPropertyChanged(nameof(songStars));
            NotifyPropertyChanged(nameof(songStatusActive));
            NotifyPropertyChanged(nameof(songEmpty));
            NotifyPropertyChanged(nameof(songActive));
        }

        private void SetValue<T>(ref T field, T value, string propertyName) {
            field = value;
            NotifyPropertyChanged(propertyName);
        }
    }
}
