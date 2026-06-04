using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using IPA.Utilities;
using LeaderboardCore.Models.UI.ViewControllers;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Core.Presentation;
using System;
using System.Reflection;
using UnityEngine;
using Zenject;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace ScoreSaber.Features.Leaderboards.UI {
    internal class PanelView : BasicDoubleTextPanelViewController {
        private const string PanelViewResource = "ScoreSaber.Features.Leaderboards.UI.PanelView.bsml";
        private const string LoadingRankingText = "<b><color=#FFDE1A>Global Ranking: </color></b> Loading...";
        private const string LoadingRankedStatusText = "Loading...";
        private const float PromptDismissNever = -1f;

        private bool _specialBackgroundMode;
        private float _specialBackgroundValue;
        private Sprite _denyahSprite;
        private ImageView _background;
        private SettingsService _settings;
        private RectTransform _promptRoot;
        private CurvedTextMeshPro _promptTextComponent;
        private LoadingControl _promptLoader;
        private RectTransform _contentRoot;
        private string _promptText = string.Empty;
        private bool _promptActive;
        private bool _promptLoading;
        private float _promptDismissRemaining = PromptDismissNever;

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

        [UIComponent("prompt-root")]
        protected RectTransform promptRoot {
            get => _promptRoot;
            set => _promptRoot = value;
        }

        [UIComponent("prompt-text-component")]
        protected CurvedTextMeshPro promptTextComponent {
            get => _promptTextComponent;
            set => _promptTextComponent = value;
        }

        [UIComponent("prompt-loader")]
        protected LoadingControl promptLoader {
            get => _promptLoader;
            set => _promptLoader = value;
        }

        [UIComponent("content-root")]
        protected RectTransform contentRoot {
            get => _contentRoot;
            set => _contentRoot = value;
        }

        [Inject]
        protected void Construct(SettingsService settings) {
            _settings = settings;
            _scoreSaberBlue = new Color(0f, 0.4705882f, 0.7254902f);
            _theWilliamGradient = new Gradient { mode = GradientMode.Blend, colorKeys = new GradientColorKey[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(new Color(1f, 0.5f, 0f), 0.17f), new GradientColorKey(Color.yellow, 0.34f), new GradientColorKey(Color.green, 0.51f), new GradientColorKey(Color.blue, 0.68f), new GradientColorKey(new Color(0.5f, 0f, 0.5f), 0.85f), new GradientColorKey(Color.red, 1.15f) } };
            backgroundColor = _scoreSaberBlue;
            topText = LoadingRankingText;
            bottomText = FormatRankedStatus(LoadingRankedStatusText);
            Plugin.Log.Debug("PanelView Setup!");
        }

        protected void OnDisable() => Disabled?.Invoke();

        public override void Parsed() {
            base.Parsed();
            IsReady = true;

            _background = outer.Background as ImageView;
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
            Ready?.Invoke();
        }

        [UIAction("clicked-settings")]
        protected void ClickedSettings() => SettingsSelected?.Invoke();

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

            SetPromptState(status ?? promptText, true, showLoadingIndicator);
            _promptDismissRemaining = dismissTime;
        }

        public void DismissPrompt(float dismissTime = 0f, float tweenTime = 0.5f) {
            if (dismissTime <= 0f) {
                SetPromptState(string.Empty, false, false);
                _promptDismissRemaining = PromptDismissNever;
                return;
            }

            _promptDismissRemaining = dismissTime;
        }

        public void Loaded(bool value) => isLoaded = value;

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
#pragma warning disable CS0618 // Type or member is obsolete
                _denyahSprite = BSMLUtilities.LoadSpriteRaw(BSMLUtilities.GetResource(Assembly.GetExecutingAssembly(), "ScoreSaber.Resources.bri-ish.png"));
#pragma warning restore CS0618 // Type or member is obsolete
            }
            _background.overrideSprite = _denyahSprite;
        }

        internal void SetLogoColor(Color color) {
            if (clickableLogo != null) {
                clickableLogo.DefaultColor = color;
            }
        }

        internal void AdvanceSpecialBackground(float deltaTime) {
            TickPrompt(deltaTime);
            if (IsReady && _specialBackgroundMode) {
                backgroundColor = _theWilliamGradient.Evaluate(_specialBackgroundValue);
                _specialBackgroundValue += deltaTime * 0.1f;
                if (_specialBackgroundValue > 1f) {
                    _specialBackgroundValue = 0f;
                }
            }
        }

        private void TickPrompt(float deltaTime) {
            if (!promptActive || _promptDismissRemaining == PromptDismissNever) {
                return;
            }

            _promptDismissRemaining -= deltaTime;
            if (_promptDismissRemaining <= 0f) {
                SetPromptState(promptText, false, false);
                _promptDismissRemaining = PromptDismissNever;
            }
        }

        private void SetPromptState(string text, bool active, bool loading) {
            _promptText = text;
            _promptActive = active;
            _promptLoading = loading;
            NotifyPropertyChanged(nameof(promptText));
            NotifyPropertyChanged(nameof(promptLoading));
            NotifyPropertyChanged(nameof(promptActive));
            NotifyPropertyChanged(nameof(contentActive));
            ApplyPromptVisibility();
        }

        private void ApplyPromptVisibility() {
            if (_promptRoot != null) {
                _promptRoot.gameObject.SetActive(promptActive);
            }

            if (_promptTextComponent != null) {
                _promptTextComponent.text = promptActive ? promptText : string.Empty;
            }

            if (_promptLoader != null) {
                _promptLoader.gameObject.SetActive(promptActive && promptLoading);
            }

            if (_contentRoot != null) {
                _contentRoot.gameObject.SetActive(contentActive);
            }
        }

        private static string FormatRankedStatus(string rankedStatus) => $"<b><color=#FFDE1A>Ranked Status:</color></b> {rankedStatus}";
    }
}
