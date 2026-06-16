using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities;
using ScoreSaber.Core.Compat;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;
using static HMUI.IconSegmentedControl;

namespace ScoreSaber.Features.Leaderboards.Adapters.LeaderboardCore {

    [HotReload(RelativePathToLayout = @"./ScoreSaberLeaderboardCoreViewController.bsml")]
    [ViewDefinition("ScoreSaber.Features.Leaderboards.Adapters.LeaderboardCore.ScoreSaberLeaderboardCoreViewController.bsml")]
    internal class ScoreSaberLeaderboardCoreViewController : BSMLAutomaticViewController {
        private static readonly FieldAccessor<LeaderboardTableView, TableView>.Accessor InnerTable = FieldAccessor<LeaderboardTableView, TableView>.GetAccessor("_tableView");
        private static readonly FieldAccessor<LeaderboardTableCell, TextMeshProUGUI>.Accessor PlayerNameText = FieldAccessor<LeaderboardTableCell, TextMeshProUGUI>.GetAccessor("_playerNameText");
        private static readonly FieldAccessor<LeaderboardTableCell, Image>.Accessor SeparatorImage = FieldAccessor<LeaderboardTableCell, Image>.GetAccessor("_separatorImage");
        private static readonly FieldAccessor<PlatformLeaderboardViewController, LoadingControl>.Accessor PlatformLoadingControl = FieldAccessor<PlatformLeaderboardViewController, LoadingControl>.GetAccessor("_loadingControl");
        private static readonly FieldAccessor<PlatformLeaderboardViewController, IconSegmentedControl>.Accessor PlatformScopeSegmentedControl = FieldAccessor<PlatformLeaderboardViewController, IconSegmentedControl>.GetAccessor("_scopeSegmentedControl");
        private static readonly FieldAccessor<PlatformLeaderboardViewController, Sprite>.Accessor PlatformGlobalLeaderboardIcon = FieldAccessor<PlatformLeaderboardViewController, Sprite>.GetAccessor("_globalLeaderboardIcon");
        private static readonly FieldAccessor<PlatformLeaderboardViewController, Sprite>.Accessor PlatformAroundPlayerLeaderboardIcon = FieldAccessor<PlatformLeaderboardViewController, Sprite>.GetAccessor("_aroundPlayerLeaderboardIcon");
        private static readonly FieldAccessor<PlatformLeaderboardViewController, Sprite>.Accessor PlatformFriendsLeaderboardIcon = FieldAccessor<PlatformLeaderboardViewController, Sprite>.GetAccessor("_friendsLeaderboardIcon");
        private const string CountryIconResource = "ScoreSaber.Resources.country.png";

        private static LoadingControl _activePlatformLoadingControl;
        private static string _suppressedPlatformCustomLevelWarningText = string.Empty;

        private LeaderboardTweeningService _leaderboardTweeningService;

        private PlatformLeaderboardViewController _platformLeaderboardViewController;
        private SettingsService _settings;
        private List<DataItem> _scopes;
        private Sprite _countryIcon;
        private TableView _innerTable;

        // bsml binds [UIComponent] to fields only on old versions (1.11.4 and earlier), so keep these as fields
        [UIComponent("leaderboard")]
        private readonly LeaderboardTableView _leaderboard = null;
        [UIComponent("scopes-segmented-control")]
        private readonly IconSegmentedControl _scopeControl = null;
        [UIComponent("main-horizontal")]
        private readonly HorizontalLayoutGroup _mainHorizontal = null;

        private LeaderboardScreenState _pendingState;
        private LeaderboardScreenScope _scope = LeaderboardScreenScope.Global;
        private int _selectedScopeCell = -1;
        private bool _scopeControlConfigured;
        private bool _suppressScopeSelection;
        private bool _upEnabled = true;
        private bool _downEnabled = true;
        private bool _isLoaded;
        private bool _scoresActive;
        private bool _errorActive;
        private float _rankColumnOffset;
        private string _errorTitle = string.Empty;
        private string _errorText = string.Empty;

        internal event Action<int> ScoreSelected;
        internal event Action<LeaderboardScreenScope> ScopeSelected;
        internal event Action PageUpRequested;
        internal event Action PageDownRequested;

        [Inject]
        private void Construct(SettingsService settings, PlatformLeaderboardViewController platformLeaderboardViewController, LeaderboardTweeningService leaderboardTweeningService) {
            _settings = settings;
            _platformLeaderboardViewController = platformLeaderboardViewController;
            _leaderboardTweeningService = leaderboardTweeningService;
        }

        [UIValue("is-loaded")]
        private bool IsLoaded {
            get => _isLoaded;
            set {
                _isLoaded = value;
                NotifyPropertyChanged(nameof(IsLoaded));
                NotifyPropertyChanged(nameof(IsLoading));
            }
        }

        [UIValue("is-loading")]
        private bool IsLoading => !IsLoaded;

        [UIValue("scores-active")]
        private bool ScoresActive {
            get => _scoresActive;
            set => SetValue(ref _scoresActive, value, nameof(ScoresActive));
        }

        [UIValue("error-active")]
        private bool ErrorActive {
            get => _errorActive;
            set => SetValue(ref _errorActive, value, nameof(ErrorActive));
        }

        [UIValue("error-text")]
        private string ErrorText {
            get => _errorText;
            set => SetValue(ref _errorText, value ?? string.Empty, nameof(ErrorText));
        }

        [UIValue("error-title")]
        private string ErrorTitle {
            get => _errorTitle;
            set => SetValue(ref _errorTitle, value ?? string.Empty, nameof(ErrorTitle));
        }

        [UIValue("up-enabled")]
        private bool UpEnabled {
            get => _upEnabled;
            set => SetValue(ref _upEnabled, value, nameof(UpEnabled));
        }

        [UIValue("down-enabled")]
        private bool DownEnabled {
            get => _downEnabled;
            set => SetValue(ref _downEnabled, value, nameof(DownEnabled));
        }

        [UIValue("scopes")]
        private List<DataItem> Scopes {
            get {
                if (_scopes == null) {
                    _scopes = new List<DataItem> {
                        new DataItem(PlatformGlobalLeaderboardIcon(ref _platformLeaderboardViewController), "Global"),
                        new DataItem(PlatformAroundPlayerLeaderboardIcon(ref _platformLeaderboardViewController), "Around You"),
                        new DataItem(PlatformFriendsLeaderboardIcon(ref _platformLeaderboardViewController), "Friends"),
                        new DataItem(CountryIcon(), "Country")
                    };
                }

                return _scopes;
            }
        }

        [UIAction("#post-parse")]
        private void Parsed() {
            ConfigureScopeControl();
            BindTableSelection();
            ApplyState(_pendingState ?? LeaderboardScreenState.Loading(1));
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            ConfigureScopeControl();
            HidePlatformControls();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling) {
            RestorePlatformControls();
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        private void SetValue<T>(ref T field, T value, string propertyName) {
            field = value;
            NotifyPropertyChanged(propertyName);
        }

        protected override void OnDestroy() {
            RestorePlatformControls();
            base.OnDestroy();
            if (_innerTable != null) {
                _innerTable.didSelectCellWithIdxEvent -= TableDidSelectCellWithIdx;
                _innerTable.didReloadDataEvent -= TableDidReloadData;
            }
            if (_scopeControl != null) {
                _scopeControl.didSelectCellEvent -= ScopeSegmentedControlDidSelectCell;
            }
        }

        [UIAction("up-clicked")]
        private void UpClicked() => PageUpRequested?.Invoke();

        [UIAction("down-clicked")]
        private void DownClicked() => PageDownRequested?.Invoke();

        internal void SetRankColumnOffset(float offset) {
            _rankColumnOffset = offset;
        }

        internal void ApplyState(LeaderboardScreenState state) {
            _pendingState = state;
            BindTableSelection();
            _leaderboardTweeningService.ClearAllTweens();
            IsLoaded = state.IsLoaded;
            UpEnabled = state.CanPageUp;
            DownEnabled = state.CanPageDown;
            ScoresActive = state.Status == LeaderboardScreenStatus.Loaded;
            ErrorActive = state.Status != LeaderboardScreenStatus.Loading && state.Status != LeaderboardScreenStatus.Loaded;
            ErrorTitle = ErrorActive ? GetErrorTitle(state.Status) : string.Empty;
            ErrorText = ErrorActive ? state.ErrorText : string.Empty;
            HidePlatformControls();

            if (_leaderboard == null) {
                return;
            }

            if (state.Status == LeaderboardScreenStatus.Loaded) {
                _leaderboard.SetScores(CreateRows(state.Leaderboard), state.PlayerScoreIndex);
                BindTableSelection();
                ConfigureVisibleCells();
                IsLoaded = true;
                ApplyFade();
                return;
            }

            if (state.Status != LeaderboardScreenStatus.Loading) {
                _leaderboard.SetScores(new List<LeaderboardTableView.ScoreData>(), -1);
            }
        }

        private List<LeaderboardTableView.ScoreData> CreateRows(LeaderboardMap leaderboard) {
            var rows = new List<LeaderboardTableView.ScoreData>();
            if (leaderboard == null) {
                return rows;
            }

            foreach (ScoreMap scoreMap in leaderboard.Scores) {
                rows.Add(new LeaderboardTableView.ScoreData(scoreMap.Score.ModifiedScore, FormatPlayerName(scoreMap), scoreMap.Score.Rank, false));
            }

            return rows;
        }

        private string FormatPlayerName(ScoreMap scoreMap) {
            bool hasMods = !string.IsNullOrEmpty(scoreMap.ModifierText);
            string name = $"<size=80%>{scoreMap.Score.Player.Name}</size>";
            string accuracy = $"<size=70%>(<color=#FFD42A>{scoreMap.Accuracy}%</color>)</size>";
            string pp = $"<size=70%>(<color=#6772E5>{scoreMap.Score.PP}<size=45%>pp</size></color>)</size>";
            string modifiers = $"<size=70%><color=#6F6F6F>[{scoreMap.ModifierText}]</color></size>";

            string formattedName = $"{name} - {accuracy}";
            if (scoreMap.Score.PP > 0 && _settings.Current.showScorePP) {
                formattedName = $"{formattedName} - {pp}";
            }

            return hasMods ? $"{formattedName} {modifiers}" : formattedName;
        }

        private static string GetErrorTitle(LeaderboardScreenStatus status) => status switch {
            LeaderboardScreenStatus.Empty => "No Scores",
            LeaderboardScreenStatus.NoPlayerScore => "No Score Yet",
            _ => "ScoreSaber Unavailable"
        };

        private void BindTableSelection() {
            if (_leaderboard == null) {
                return;
            }

            if (_innerTable == null) {
                LeaderboardTableView leaderboard = _leaderboard;
                _innerTable = InnerTable(ref leaderboard);
            }

            if (_innerTable == null) {
                return;
            }

            _innerTable.selectionType = TableViewSelectionType.Single;
            _innerTable.didSelectCellWithIdxEvent -= TableDidSelectCellWithIdx;
            _innerTable.didSelectCellWithIdxEvent += TableDidSelectCellWithIdx;
            _innerTable.didReloadDataEvent -= TableDidReloadData;
            _innerTable.didReloadDataEvent += TableDidReloadData;
        }

        private void TableDidSelectCellWithIdx(TableView tableView, int index) {
            tableView.ClearSelection();
            SelectScore(index);
        }

        private void TableDidReloadData(TableView tableView) => ConfigureVisibleCells();

        private void ConfigureVisibleCells() {
            if (_innerTable == null) {
                return;
            }

            foreach (TableCell cell in _innerTable.visibleCells) {
                if (!(cell is LeaderboardTableCell leaderboardCell)) {
                    continue;
                }

                EnableRichText(leaderboardCell);
                ApplyPlayerNameLayout(leaderboardCell);
                ApplySeparator(leaderboardCell);
                ConfigureClickHandler(leaderboardCell);
            }
        }

        private static void ApplySeparator(LeaderboardTableCell cell) {
            Image separator = SeparatorImage(ref cell);
            if (separator == null) {
                return;
            }

            separator.gameObject.SetActive(true);
            separator.enabled = true;
        }

        private void ConfigureClickHandler(LeaderboardTableCell cell) {
            LeaderboardRowClickHandler clickHandler = cell.gameObject.GetComponent<LeaderboardRowClickHandler>();
            if (clickHandler == null) {
                clickHandler = cell.gameObject.AddComponent<LeaderboardRowClickHandler>();
            }

            clickHandler.Configure(cell.idx, _innerTable, SeparatorImage(ref cell) as ImageView);
        }

        private static void EnableRichText(LeaderboardTableCell cell) {
            TextMeshProUGUI playerNameText = PlayerNameText(ref cell);
            if (playerNameText == null) {
                return;
            }

            playerNameText.richText = true;
            playerNameText.text = playerNameText.text;
            playerNameText.SetVerticesDirty();
        }

        private void ApplyPlayerNameLayout(LeaderboardTableCell cell) {
            TextMeshProUGUI playerNameText = PlayerNameText(ref cell);
            if (playerNameText == null) {
                return;
            }

            RectTransform nameTransform = playerNameText.rectTransform;
            LeaderboardPlayerNameLayout layout = nameTransform.GetComponent<LeaderboardPlayerNameLayout>();
            if (layout == null) {
                layout = nameTransform.gameObject.AddComponent<LeaderboardPlayerNameLayout>();
                layout.Capture(nameTransform);
            }

            layout.Apply(nameTransform, _rankColumnOffset);
        }

        private void ApplyFade() {
            _leaderboardTweeningService.FadeLayoutGroup($"leaderboard_fade_{_leaderboard.GetInstanceID()}", 0f, 1f, 0.5f, _mainHorizontal);
        }

        private void SelectScore(int index) => ScoreSelected?.Invoke(index);

        private void ScopeClicked(LeaderboardScreenScope scope) {
            _scope = scope;
            SelectScopeIcon(scope);
            ScopeSelected?.Invoke(scope);
        }

        private void SelectScopeIcon(LeaderboardScreenScope scope) {
            if (_scopeControl == null) {
                return;
            }

            int cellNumber = CellForScope(scope);
            if (_selectedScopeCell == cellNumber) {
                return;
            }

            _selectedScopeCell = cellNumber;
            _suppressScopeSelection = true;
            _scopeControl.SelectCellWithNumber(cellNumber);
            _suppressScopeSelection = false;
        }

        private void ConfigureScopeControl() {
            if (_scopeControl == null) {
                return;
            }

            if (!_scopeControlConfigured) {
                HorizontalLayoutGroup horizontalGroup = _scopeControl.GetComponent<HorizontalLayoutGroup>();
                if (horizontalGroup != null) {
                    DestroyImmediate(horizontalGroup);
                }
                if (_scopeControl.GetComponent<VerticalLayoutGroup>() == null) {
                    _scopeControl.gameObject.AddComponent<VerticalLayoutGroup>();
                }

                _scopeControl.didSelectCellEvent -= ScopeSegmentedControlDidSelectCell;
                _scopeControl.didSelectCellEvent += ScopeSegmentedControlDidSelectCell;
                _scopeControlConfigured = true;
            }

            SelectScopeIcon(_scope);
        }

        private void ScopeSegmentedControlDidSelectCell(SegmentedControl segmentedControl, int cellNumber) {
            if (_suppressScopeSelection) {
                return;
            }

            ScopeClicked(ScopeForCell(cellNumber));
        }

        private static int CellForScope(LeaderboardScreenScope scope) => scope switch {
            LeaderboardScreenScope.AroundPlayer => 1,
            LeaderboardScreenScope.Friends => 2,
            LeaderboardScreenScope.Country => 3,
            _ => 0
        };

        private static LeaderboardScreenScope ScopeForCell(int cellNumber) => cellNumber switch {
            1 => LeaderboardScreenScope.AroundPlayer,
            2 => LeaderboardScreenScope.Friends,
            3 => LeaderboardScreenScope.Country,
            _ => LeaderboardScreenScope.Global
        };

        private Sprite CountryIcon() {
            if (_countryIcon == null) {
                Texture2D countryTexture = new Texture2D(64, 64);
                countryTexture.LoadImage(BsmlCompat.GetResource(Assembly.GetExecutingAssembly(), CountryIconResource));
                countryTexture.Apply();
                _countryIcon = Sprite.Create(countryTexture, new Rect(0, 0, countryTexture.width, countryTexture.height), Vector2.zero);
            }

            return _countryIcon;
        }

        private void HidePlatformControls() {
            _activePlatformLoadingControl = _platformLeaderboardViewController != null ? PlatformLoadingControl(ref _platformLeaderboardViewController) : null;
            _activePlatformLoadingControl?.Hide();

            IconSegmentedControl scopeSegmentedControl = _platformLeaderboardViewController != null ? PlatformScopeSegmentedControl(ref _platformLeaderboardViewController) : null;
            if (scopeSegmentedControl != null) {
                scopeSegmentedControl.gameObject.SetActive(false);
            }
        }

        private void RestorePlatformControls() {
            LoadingControl loadingControl = _activePlatformLoadingControl;
            _activePlatformLoadingControl = null;
            if (loadingControl != null && IsCustomLevelWarningText(_suppressedPlatformCustomLevelWarningText)) {
                loadingControl.ShowText(_suppressedPlatformCustomLevelWarningText, false);
            }
            _suppressedPlatformCustomLevelWarningText = string.Empty;

            IconSegmentedControl scopeSegmentedControl = _platformLeaderboardViewController != null ? PlatformScopeSegmentedControl(ref _platformLeaderboardViewController) : null;
            if (scopeSegmentedControl != null) {
                scopeSegmentedControl.gameObject.SetActive(true);
            }
        }

        internal static bool ShouldSuppressPlatformCustomLevelWarning(LoadingControl loadingControl, string text) {
            if (loadingControl == null || loadingControl != _activePlatformLoadingControl || !IsCustomLevelWarningText(text)) {
                return false;
            }

            _suppressedPlatformCustomLevelWarningText = text;
            loadingControl.Hide();
            return true;
        }

        private static bool IsCustomLevelWarningText(string text) => !string.IsNullOrEmpty(text) && text.IndexOf("custom levels", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal class LeaderboardPlayerNameLayout : MonoBehaviour {
        private Vector2 _offsetMin;
        private Vector2 _offsetMax;
        private bool _captured;

        internal void Capture(RectTransform transform) {
            if (_captured) {
                return;
            }

            _offsetMin = transform.offsetMin;
            _offsetMax = transform.offsetMax;
            _captured = true;
        }

        internal void Apply(RectTransform transform, float offset) {
            if (!_captured) {
                Capture(transform);
            }

            transform.offsetMin = new Vector2(_offsetMin.x + offset, _offsetMin.y);
            transform.offsetMax = _offsetMax;
        }
    }

    internal class LeaderboardRowClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
        private static readonly Color NormalColor0 = new Color(1f, 1f, 1f, 0.2509804f);
        private static readonly Color NormalColor1 = new Color(1f, 1f, 1f, 0f);

        private Graphic _graphic;
        private ImageView _separator;
        private Vector3 _separatorScale;
        private TableView _tableView;
        private int _index;
        private bool _isScaled;

        internal void Configure(int index, TableView tableView, ImageView separator) {
            _index = index;
            _tableView = tableView;
            _separator = separator;

            if (_graphic == null) {
                _graphic = gameObject.GetComponent<Graphic>();
            }

            if (_graphic != null) {
                _graphic.raycastTarget = true;
            }

            if (_separator != null && _separatorScale == Vector3.zero) {
                _separatorScale = _separator.transform.localScale;
            }
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (_tableView == null || !ContainsPointer(eventData)) {
                return;
            }

            BeatSaberUI.BasicUIAudioManager?.HandleButtonClickEvent();
            _tableView.SelectCellWithIdx(_index, true);
        }

        public void OnPointerEnter(PointerEventData eventData) {
            if (_separator == null) {
                return;
            }

            SetSeparatorScaled(true);
            FadeSeparator(Color.white, Color.white, NormalColor1, 0.15f);
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (_separator == null) {
                return;
            }

            SetSeparatorScaled(false);
            FadeSeparator(Color.white, NormalColor0, NormalColor1, 0.05f);
        }

        private void SetSeparatorScaled(bool isScaled) {
            if (_isScaled == isScaled) {
                return;
            }

            _separator.transform.localScale = isScaled ? _separatorScale * 1.8f : _separatorScale;
            _isScaled = isScaled;
        }

        private void FadeSeparator(Color targetColor, Color targetColor0, Color targetColor1, float duration) {
            StopAllCoroutines();
            StartCoroutine(LerpColors(
                _separator,
                _separator.color,
                targetColor,
                _separator.color0,
                targetColor0,
                _separator.color1,
                targetColor1,
                duration));
        }

        private bool ContainsPointer(PointerEventData eventData) {
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null) {
                return true;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint)) {
                return false;
            }

            Rect rect = rectTransform.rect;
            float rowHalfHeight = _tableView != null ? _tableView.cellSize * 0.5f : rect.height * 0.5f;
            float rowCenterY = rect.center.y;
            return localPoint.x >= rect.xMin &&
                localPoint.x <= rect.xMax &&
                localPoint.y >= rowCenterY - rowHalfHeight &&
                localPoint.y <= rowCenterY + rowHalfHeight;
        }

        private static IEnumerator LerpColors(
            ImageView target,
            Color startColor,
            Color endColor,
            Color startColor0,
            Color endColor0,
            Color startColor1,
            Color endColor1,
            float duration) {

            float elapsedTime = 0f;
            while (elapsedTime < duration) {
                float t = elapsedTime / duration;
                target.color = Color.Lerp(startColor, endColor, t);
                target.color0 = Color.Lerp(startColor0, endColor0, t);
                target.color1 = Color.Lerp(startColor1, endColor1, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            target.color = endColor;
            target.color0 = endColor0;
            target.color1 = endColor1;
        }
    }
}
