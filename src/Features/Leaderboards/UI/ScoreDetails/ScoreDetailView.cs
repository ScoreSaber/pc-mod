using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Core;
using System;
using UnityEngine.UI;

namespace ScoreSaber.Features.Leaderboards.UI.ScoreDetails {
    internal class ScoreDetailView {

        #region BSML Components
        [UIComponent("detail-modal-root")]
        public ModalView detailModalRoot = null;
        [UIComponent("prefix-text")]
        protected readonly CurvedTextMeshPro _prefixText = null;
        [UIComponent("name-text")]
        protected readonly CurvedTextMeshPro _nameText = null;
        [UIComponent("device-hmd-text")]
        protected readonly CurvedTextMeshPro _deviceHMDText = null;
        /* TODO: readd once we improve our controller detection
        [UIComponent("devicecontrollerleft-text")]
        protected readonly CurvedTextMeshPro _deviceControllerLeftText = null;
        [UIComponent("devicecontrollerright-text")]
        protected readonly CurvedTextMeshPro _deviceControllerRightText = null;*/
        [UIComponent("score-text")]
        protected readonly CurvedTextMeshPro _scoreText = null;
        [UIComponent("pp-text")]
        protected readonly CurvedTextMeshPro _ppText = null;
        [UIComponent("max-combo-text")]
        protected readonly CurvedTextMeshPro _maxComboText = null;
        [UIComponent("full-combo-text")]
        protected readonly CurvedTextMeshPro _fullComboText = null;
        [UIComponent("bad-cuts-text")]
        protected readonly CurvedTextMeshPro _badCutsText = null;
        [UIComponent("missed-notes-text")]
        protected readonly CurvedTextMeshPro _missedNotesText = null;
        [UIComponent("modifiers-text")]
        protected readonly CurvedTextMeshPro _modifiersText = null;
        [UIComponent("time-text")]
        protected readonly CurvedTextMeshPro _timeText = null;

        [UIComponent("prefix-image")]
        private readonly ImageView _scoreInfoPrefixPicture = null;
        public string scoreInfoPrefixPicture {
            set {
                bool hasImage = value != null;
                _scoreInfoPrefixPicture.gameObject.SetActive(hasImage);
                if (hasImage) {
                    _scoreInfoPrefixPicture.SetImageAsync(value).RunTask();
                }
            }
        }

        private readonly HoverHint _scoreInfoHoverHint = null;
        public HoverHint scoreInfoHoverHint => _scoreInfoHoverHint ?? _scoreInfoPrefixPicture.gameObject.GetComponent<HoverHint>();

        [UIComponent("watch-replay-button")]
        protected readonly Button _watchReplayButton = null;
        [UIComponent("show-profile-button")]
        protected readonly Button _showProfileButton = null;
        [UIAction("show-profile-click")]
        private void ShowProfileClicked() {
            if (_currentScore != null) {
                showProfile?.Invoke(_currentScore.PlayerId);
            }
        }
        [UIAction("replay-click")] private void ReplayClicked() => StartReplay();
        #endregion

        public event Action<string> showProfile;
        public event Action<ScoreMap> startReplay;

        private bool _allowReplayWatching = true;

        private ScoreDetailData _currentScore { get; set; }

        [UIAction("#post-parse")]
        public void Parsed() {
            _nameText.fontSizeMin = 2.5f;
            _nameText.fontSizeMax = 4.0f;
            _nameText.enableAutoSizing = true;
            _watchReplayButton.transform.localScale *= .4f;
            _showProfileButton.transform.localScale *= .4f;
        }

        public void SetScoreInfo(ScoreMap scoreMap, bool replayDownloading) {

            _currentScore = ScoreDetailData.Create(scoreMap);
            ApplyCrown(_currentScore);
            _nameText.text = _currentScore.PlayerNameText;
            SetFancyText(_deviceHMDText, "HMD", _currentScore.DeviceHMDText);
            //SetFancyText(_deviceControllerLeftText, "Left Controller", score.deviceControllerLeft ?? "N/A");
            //SetFancyText(_deviceControllerRightText, "Right Controller", score.deviceControllerRight ?? "N/A");
            SetFancyText(_scoreText, "Score", _currentScore.ScoreText);
            SetFancyText(_ppText, "Performance Points", _currentScore.PPText);
            SetFancyText(_maxComboText, "Combo", _currentScore.MaxComboText);
            SetFancyText(_fullComboText, "Full Combo", _currentScore.FullComboText);
            SetFancyText(_badCutsText, "Bad Cuts", _currentScore.BadCutsText);
            SetFancyText(_missedNotesText, "Missed Notes", _currentScore.MissedNotesText);
            SetFancyText(_modifiersText, "Modifiers", _currentScore.ModifiersText);
            SetFancyText(_timeText, "Time Set", _currentScore.TimeSetText);

            if (!replayDownloading) {
                SetButtonState(_watchReplayButton, _currentScore.HasReplay && _allowReplayWatching);
            }
        }

        private void ApplyCrown(ScoreDetailData score) {

            scoreInfoPrefixPicture = null;
            scoreInfoHoverHint.enabled = score.HasCrown;

            if (!score.HasCrown) {
                return;
            }

            scoreInfoPrefixPicture = score.CrownImage;
            scoreInfoHoverHint.text = score.CrownDescription;
        }

        private void StartReplay() {
            if (_currentScore == null) {
                return;
            }

            _watchReplayButton.interactable = false;
            startReplay?.Invoke(_currentScore.Score);
        }

        public void AllowReplayWatching(bool value) {
            _allowReplayWatching = value;

            SetButtonState(_watchReplayButton, value && _currentScore != null && _currentScore.HasReplay);
        }

        private void SetButtonState(Button button, bool value) {

            if (button == null)
                return;

            button.interactable = value;
            button.gameObject.GetComponent<HoverHint>().enabled = value;
        }

        private static void SetFancyText(CurvedTextMeshPro text, string title, string value) => text.text = $"<color=#6F6F6F>{title}:</color> {value}";
    }
}
