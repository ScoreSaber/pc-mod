using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using IPA.Utilities;
using LeaderboardCore.Models.UI.ViewControllers;
using ScoreSaber.Core.Compat;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Core.Presentation;
using ScoreSaber.Features.Leaderboards.Services;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ScoreSaber.Features.Leaderboards.UI {
    internal class PanelView : BasicDoubleTextPanelViewController {
        private const string PanelViewResource = "ScoreSaber.Features.Leaderboards.UI.PanelView.bsml";
        private const string LoadingRankingText = "<b><color=#FFDE1A>Global Ranking: </color></b> Loading...";
        private const string LoadingRankedStatusText = "Loading...";
        private const float PromptDismissNever = -1f;
        private const float PromptHiddenY = 4.3f;
        private const float PromptVisibleY = 10.3f;
        private const float PromptTweenTime = 0.5f;
        private const string PanelPromptShowTweenId = "panel_prompt_show";
        private const string PanelPromptDismissTweenId = "panel_prompt_dismiss";

        private bool _specialBackgroundMode;
        private float _specialBackgroundValue;
        private Sprite _denyahSprite;
        private ImageView _background;
        private SettingsService _settings;
        private LeaderboardTweeningService _leaderboardTweeningService;

        // bsml binds [UIComponent]/[UIObject] to fields only on old versions (1.11.4 and earlier), so keep these as fields
        [UIComponent("tournament-actions-root")]
        protected readonly RectTransform _tournamentActionsRoot = null;
        [UIComponent("compete-button")]
        protected readonly ClickableImage _competeButton = null;
        [UIComponent("prompt-root")]
        protected readonly RectTransform _promptRoot = null;
        [UIComponent("prompt-text-component")]
        protected readonly CurvedTextMeshPro _promptTextComponent = null;
        [UIObject("prompt-loader-slot")]
        protected readonly GameObject _promptLoaderSlot = null;
        [UIComponent("content-root")]
        protected readonly RectTransform _contentRoot = null;
        private string _promptText = string.Empty;
        private bool _promptActive;
        private bool _promptLoading;
        private bool _competeButtonActive;

        internal bool IsReady { get; private set; }

        private Color _scoreSaberBlue;
        private Gradient _theWilliamGradient;
        internal static readonly FieldAccessor<ImageView, float>.Accessor ImageSkew = FieldAccessor<ImageView, float>.GetAccessor("_skew");
        internal static readonly FieldAccessor<ImageView, bool>.Accessor ImageGradient = FieldAccessor<ImageView, bool>.GetAccessor("_gradient");

        internal event Action Ready;
        internal event Action Disabled;
        internal event Action LogoSelected;
        internal event Action SettingsSelected;
        internal event Action RankingSelected;
        internal event Action StatusSelected;
        internal event Action CompeteSelected;

        protected override string customBSML => BSMLHotReload.ResourceContent(typeof(PanelView).Assembly, PanelViewResource);

        protected override bool IsLogoClickable => true;

        protected override string LogoSource => "ScoreSaber.Resources.logo.png";

        protected override string LogoHoverHint => "Opens the ScoreSaber main menu";

        protected override bool IsTopTextClickable => true;

        protected override string TopHoverHint => "View Profile";

        protected override bool IsBottomTextClickable => true;

        protected override string BottomHoverHint => "Opens in browser";

        [UIValue("prompt-text")]
        protected string promptText {
            get => _promptText;
            set {
                _promptText = value;
                NotifyPropertyChanged();
                ApplyPromptVisibility();
            }
        }

        [UIValue("prompt-active")]
        protected bool promptActive {
            get => _promptActive;
            set {
                _promptActive = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(contentActive));
                ApplyPromptVisibility();
            }
        }

        [UIValue("content-active")]
        protected bool contentActive => true;

        [UIValue("prompt-loader-active")]
        protected bool promptLoading {
            get => _promptLoading;
            set {
                _promptLoading = value;
                NotifyPropertyChanged();
                ApplyPromptVisibility();
            }
        }

        protected bool competeButtonActive {
            get => _competeButtonActive;
            set {
                if (_competeButtonActive == value) {
                    return;
                }

                _competeButtonActive = value;
                ApplyTournamentActionVisibility();
            }
        }

        [Inject]
        protected void Construct(SettingsService settings, LeaderboardTweeningService leaderboardTweeningService) {
            _settings = settings;
            _leaderboardTweeningService = leaderboardTweeningService;
            _scoreSaberBlue = new Color(0f, 0.4705882f, 0.7254902f);
            _theWilliamGradient = new Gradient { mode = GradientMode.Blend, colorKeys = new GradientColorKey[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(new Color(1f, 0.5f, 0f), 0.17f), new GradientColorKey(Color.yellow, 0.34f), new GradientColorKey(Color.green, 0.51f), new GradientColorKey(Color.blue, 0.68f), new GradientColorKey(new Color(0.5f, 0f, 0.5f), 0.85f), new GradientColorKey(Color.red, 1.15f) } };
            backgroundColor = _scoreSaberBlue;
            topText = LoadingRankingText;
            bottomText = FormatRankedStatus(LoadingRankedStatusText);
            Plugin.Log.Debug("PanelView Setup!");
        }

        protected void OnDisable() {
            KillPromptTweens();
            Disabled?.Invoke();
        }

        public override void Parsed() {
            base.Parsed();
            IsReady = true;

            _background = outer.GetBackground() as ImageView;
            if (_background != null) {
                _background.color0 = Color.white;
                _background.color1 = new Color(1f, 1f, 1f, 0f);
                ImageGradient(ref _background) = true;
                ImageSkew(ref _background) = 0.18f;
                _background.enabled = false;
                _background.enabled = true;
            }

            if (clickableLogo != null) {
                clickableLogo.name = "ScoreSaberLogoImage";
                clickableLogo.SetVerticesDirty();
            }

            if (separator != null) {
                separator.name = "Separator";
                ImageSkew(ref separator) = 0.18f;
                separator.SetVerticesDirty();
            }

            ApplyPromptVisibility();
            ApplyTournamentActionVisibility();
            Ready?.Invoke();
        }

        [UIAction("clicked-settings")]
        protected void ClickedSettings() => SettingsSelected?.Invoke();

        [UIAction("clicked-compete")]
        protected void ClickedCompete() {
            if (competeButtonActive) {
                CompeteSelected?.Invoke();
            }
        }

        protected override void OnLogoClicked() => LogoSelected?.Invoke();

        protected override void OnTopClicked() => RankingSelected?.Invoke();

        protected override void OnBottomClicked() => StatusSelected?.Invoke();

        public void SetRankedStatus(string rankedStatus) => bottomText = FormatRankedStatus(rankedStatus);

        public void SetPromptInfo(string status, bool showLoadingIndicator, float dismissTime = PromptDismissNever) => SetPrompt(status, showLoadingIndicator, dismissTime);

        public void SetPromptError(string status, bool showLoadingIndicator, float dismissTime = PromptDismissNever) => SetPrompt($"<color=#fc8181>{status}</color>", showLoadingIndicator, dismissTime);

        public void SetPromptSuccess(string status, bool showLoadingIndicator, float dismissTime = PromptDismissNever) => SetPrompt($"<color=#89fc81>{status}</color>", showLoadingIndicator, dismissTime);

        public void SetPrompt(string status, bool showLoadingIndicator, float dismissTime = PromptDismissNever) {
            if (!_settings.Current.showStatusText) {
                return;
            }

            bool wasActive = promptActive;
            if (wasActive) {
                KillPromptTweens();
                SetPromptVisible();
            }

            SetPromptState(status ?? promptText, true, showLoadingIndicator);
            if (dismissTime != PromptDismissNever) {
                DismissPrompt(dismissTime);
            }
        }

        public void DismissPrompt(float dismissTime = 0f, float tweenTime = 0.5f) {
            if (_promptRoot == null || !promptActive) {
                return;
            }

            _leaderboardTweeningService?.KillTween(PanelPromptDismissTweenId);
            float startValue = 1f;
            if (dismissTime <= 0f) {
                _leaderboardTweeningService?.KillTween(PanelPromptShowTweenId);
                startValue = GetPromptCanvasGroup().alpha;
            }
            _leaderboardTweeningService?.CreatePromptTween(PanelPromptDismissTweenId, startValue, 0f, tweenTime, dismissTime, _promptRoot, PromptHiddenY, PromptVisibleY, SetPromptInactive);
        }

        public void DismissLoadingPrompt() {
            if (promptLoading) {
                DismissPrompt();
            }
        }

        public void Loaded(bool value) => isLoaded = value;

        internal void SetTournamentActionsVisible(bool canCompete) {
            competeButtonActive = canCompete;
            ApplyTournamentActionVisibility();
        }

        internal void SetGlobalLeaderboardRanking(string text) => topText = text;

        internal void SetWilliumsMode(bool value) {
            _specialBackgroundMode = value;
            if (!value && _background != null) {
                backgroundColor = _scoreSaberBlue;
            }
        }

        internal void SetDenyahMode(bool value) {
            if (_background == null) {
                return;
            }

            if (!value) {
                backgroundColor = _scoreSaberBlue;
                _background.overrideSprite = null;
                return;
            }

            if (_denyahSprite == null) {
                _denyahSprite = BsmlCompat.LoadSpriteRaw(BsmlCompat.GetResource(Assembly.GetExecutingAssembly(), "ScoreSaber.Resources.bri-ish.png"));
            }
            _background.overrideSprite = _denyahSprite;
        }

        internal void SetLogoColor(Color color) {
            if (clickableLogo != null) {
                clickableLogo.DefaultColor = color;
            }
        }

        internal void AdvanceSpecialBackground(float deltaTime) {
            if (IsReady && _specialBackgroundMode) {
                backgroundColor = _theWilliamGradient.Evaluate(_specialBackgroundValue);
                _specialBackgroundValue += deltaTime * 0.1f;
                if (_specialBackgroundValue > 1f) {
                    _specialBackgroundValue = 0f;
                }
            }
        }

        private void SetPromptState(string text, bool active, bool loading) {
            bool wasActive = _promptActive;
            _promptText = text;
            _promptActive = active;
            _promptLoading = loading;
            NotifyPropertyChanged(nameof(promptText));
            NotifyPropertyChanged(nameof(promptLoading));
            NotifyPropertyChanged(nameof(promptActive));
            NotifyPropertyChanged(nameof(contentActive));
            ApplyPromptVisibility();

            if (active && !wasActive) {
                AnimatePromptIn();
            }
        }

        private void SetPromptInactive() {
            _promptActive = false;
            _promptLoading = false;
            NotifyPropertyChanged(nameof(promptLoading));
            NotifyPropertyChanged(nameof(promptActive));
            NotifyPropertyChanged(nameof(contentActive));
            ApplyPromptVisibility();
        }

        private void ApplyPromptVisibility() {
            if (_promptRoot != null) {
                _promptRoot.gameObject.SetActive(promptActive);
                if (!promptActive) {
                    SetPromptHidden();
                }
            }

            if (_promptTextComponent != null) {
                _promptTextComponent.text = promptActive ? promptText : string.Empty;
            }

            if (_promptLoaderSlot != null) {
                _promptLoaderSlot.SetActive(promptActive && promptLoading);
            }

            if (_contentRoot != null) {
                _contentRoot.gameObject.SetActive(contentActive);
            }
        }

        private void AnimatePromptIn() {
            if (_promptRoot == null || _leaderboardTweeningService == null) {
                return;
            }

            KillPromptTweens();
            _leaderboardTweeningService.CreatePromptTween(PanelPromptShowTweenId, 0f, 1f, PromptTweenTime, 0f, _promptRoot, PromptHiddenY, PromptVisibleY);
        }

        private void SetPromptHidden() {
            KillPromptTweens();
            if (_promptRoot == null) {
                return;
            }

            _promptRoot.localPosition = new Vector3(_promptRoot.localPosition.x, PromptHiddenY, _promptRoot.localPosition.z);
            GetPromptCanvasGroup().alpha = 0f;
        }

        private void SetPromptVisible() {
            if (_promptRoot == null) {
                return;
            }

            _promptRoot.localPosition = new Vector3(_promptRoot.localPosition.x, PromptVisibleY, _promptRoot.localPosition.z);
            GetPromptCanvasGroup().alpha = 1f;
        }

        private CanvasGroup GetPromptCanvasGroup() {
            CanvasGroup canvasGroup = _promptRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) {
                canvasGroup = _promptRoot.gameObject.AddComponent<CanvasGroup>();
            }

            return canvasGroup;
        }

        private void KillPromptTweens() {
            _leaderboardTweeningService?.KillTween(PanelPromptShowTweenId);
            _leaderboardTweeningService?.KillTween(PanelPromptDismissTweenId);
        }

        private void ApplyTournamentActionVisibility() {
            if (_tournamentActionsRoot != null) {
                _tournamentActionsRoot.gameObject.SetActive(true);
            }

            if (_competeButton != null) {
                _competeButton.gameObject.SetActive(competeButtonActive);
            }
        }

        private static string FormatRankedStatus(string rankedStatus) => $"<b><color=#FFDE1A>Ranked Status:</color></b> {rankedStatus}";
    }
}
