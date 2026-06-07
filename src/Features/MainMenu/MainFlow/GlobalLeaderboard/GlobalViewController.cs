using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using ScoreSaber.Core.Api;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Core;
using ScoreSaber.Core.Presentation;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Features.Players.Services;
using ScoreSaber.Features.Players.Profile;
using ScoreSaber.Features.Leaderboards.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ScoreSaber.Features.MainMenu.MainFlow.GlobalLeaderboard {
    [HotReload]
    internal class GlobalViewController : BSMLAutomaticViewController {

        #region UI Properties

        [UIParams]
        protected readonly BSMLParserParams _parserParams = null;

        [UIComponent("up-button")]
        protected readonly Button _upButton = null;

        [UIComponent("down-button")]
        protected readonly Button _downButton = null;

        [UIComponent("profile-modal")]
        protected readonly ProfileDetailView _profileDetailView = null;

        [UIComponent("dismiss-button")]
        protected readonly Button _dismissButton = null;

        [UIComponent("more-info-button")]
        protected readonly Button _moreInfoButton = null;

        [UIValue("global-host")]
        protected GlobalLeaderboardHost globalHost => _globalLeaderboardHost;

        [UIComponent("global-scope")]
        protected readonly ClickableImage _globalScopeImage = null;
        [UIComponent("player-scope")]
        protected readonly ClickableImage _playerScopeImage = null;
        [UIComponent("friends-scope")]
        protected readonly ClickableImage _friendsScopeImage = null;
        [UIComponent("location-scope")]
        protected readonly ClickableImage _countryScopeImage = null;

        private readonly Color _selectedColor = new Color(0.60f, 0.80f, 1);
        #endregion

        #region Handlers
        [UIAction("global-up")] private void GlobalUpClicked() => PageButtonClicked(false);
        [UIAction("global-down")] private void GlobalDownClicked() => PageButtonClicked(true);
        [UIAction("global-click")] private void GlobalTextClicked() => Application.OpenURL(ScoreSaberUrls.GlobalLeaderboard());

        [UIAction("global-scope-click")] private void GlobalScopeClicked() => ScopeClicked(GlobalPlayerScope.Global);
        [UIAction("player-scope-click")] private void PlayerScopeClicked() => ScopeClicked(GlobalPlayerScope.AroundPlayer);
        [UIAction("friends-scope-click")] private void FriendsScopeClicked() => ScopeClicked(GlobalPlayerScope.Friends);

        [UIAction("location-scope-click")]
        private void LocationScopeClicked() => ScopeClicked(LocationScope());
        [UIAction("more-info-click")] private void MoreInfoClicked() => Application.OpenURL("https://wiki.scoresaber.com/ranking-system.html");
        #endregion

        private DiContainer _container = null;
        private GlobalPlayerQueryService _globalPlayerQueryService = null;
        private GlobalPlayerSession _globalPlayerSession = null;
        private GlobalLeaderboardHost _globalLeaderboardHost = null;
        private SettingsService _settings = null;
        private ScoreSaberUIMaterials _materials = null;
        private CancellationTokenSource _refreshCancellation = null;

        [Inject]
        protected void Construct(
            DiContainer container,
            GlobalPlayerQueryService globalPlayerQueryService,
            GlobalPlayerSession globalPlayerSession,
            GlobalLeaderboardHost globalLeaderboardHost,
            SettingsService settings,
            ScoreSaberUIMaterials materials) {

            _container = container;
            _globalPlayerQueryService = globalPlayerQueryService;
            _globalPlayerSession = globalPlayerSession;
            _globalLeaderboardHost = globalLeaderboardHost;
            _settings = settings;
            _materials = materials;
            Plugin.Log.Debug("GlobalViewController Setup");
        }

        [UIAction("#post-parse")]
        public void Parsed() {

            _upButton.transform.localScale *= .7f;
            _downButton.transform.localScale *= .7f;
            SelectScopeIcon(_globalPlayerSession.Scope);
            UpdateNavigationButtons();


            Button[] buttons = new Button[2] { _dismissButton, _moreInfoButton };
            foreach (var button in buttons) {
                foreach (var imageView in button.GetComponentsInChildren<ImageView>()) {
                    var image = imageView;
                    PanelView.ImageSkew(ref image) = 0f;
                }
            }

            _container.Inject(_profileDetailView);
            RefreshDelayed().RunTask();
        }

        private void ScopeClicked(GlobalPlayerScope scope) {

            if (!_globalPlayerSession.SelectScope(scope)) {
                return;
            }

            SelectScopeIcon(_globalPlayerSession.Scope);
            UpdateNavigationButtons();
            RefreshDelayed().RunTask();
        }

        private async Task RefreshDelayed() {

            CancelRefresh();
            _refreshCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = _refreshCancellation.Token;
            _globalLeaderboardHost.SetLoading(true);

            string localRequestId = _globalPlayerSession.BeginRequest();
            try {
                GlobalPlayerPage page = await _globalPlayerQueryService.GetPlayerPage(_globalPlayerSession.Scope, _globalPlayerSession.Page, cancellationToken);
                if (_globalPlayerSession.IsCurrentRequest(localRequestId) && page != null) {
                    _globalLeaderboardHost.SetCells(CreateCells(page));
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Plugin.Log.Error($"Failed to load global leaderboard: {ex}");
            }
        }

        private List<GlobalCell> CreateCells(GlobalPlayerPage page) {
            var cells = new List<GlobalCell>();
            for (int i = 0; i < page.Players.Length; i++) {
                PlayerSummary player = page.Players[i];
                cells.Add(new GlobalCell(
                    _materials,
                    player.Id,
                    player.Avatar,
                    player.Name,
                    player.Country,
                    FormatRank(page, i, player.Stats.Rank),
                    player.Stats.TotalPP,
                    OnGlobalCellClicked));
            }
            return cells;
        }

        private static string FormatRank(GlobalPlayerPage page, int index, int playerRank) {
            if (page.Scope == GlobalPlayerScope.Country || page.Scope == GlobalPlayerScope.Region || page.Scope == GlobalPlayerScope.Friends) {
                int localRank = index + 1 + ((page.Page - 1) * 5);
                return string.Format("#{0:n0} (#{1:n0})", localRank, playerRank);
            }
            return string.Format("#{0:n0}", playerRank);
        }

        private void OnGlobalCellClicked(string identifier, string name) {
            ShowProfile(identifier, name).RunTask();
        }

        public async Task ShowProfile(string playerId, string name) {

            _parserParams.EmitEvent("close-modals");
            _parserParams.EmitEvent("show-profile");
            _profileDetailView.SetLoadingState(true);
            _profileDetailView.name = name;
            try {
                await _profileDetailView.ShowProfile(playerId);
            } catch (Exception) {
                Plugin.Log.Error("Failed to load player stats, bad internet connection");
            }
        }

        private void PageButtonClicked(bool down) {

            _globalPlayerSession.MovePage(down);
            UpdateNavigationButtons();
            RefreshDelayed().RunTask();
        }

        private void UpdateNavigationButtons() {
            if (_globalPlayerSession.Scope == GlobalPlayerScope.AroundPlayer) {
                _upButton.interactable = false;
                _downButton.interactable = false;
                return;
            }

            _upButton.interactable = _globalPlayerSession.Page > 1;
            _downButton.interactable = true;
        }

        private void SelectScopeIcon(GlobalPlayerScope scope) {
            _globalScopeImage.DefaultColor = Color.white;
            _playerScopeImage.DefaultColor = Color.white;
            _friendsScopeImage.DefaultColor = Color.white;
            _countryScopeImage.DefaultColor = Color.white;

            ScopeImage(scope).DefaultColor = _selectedColor;
        }

        private GlobalPlayerScope LocationScope() {
            switch (_settings.Current.locationFilterMode.ToLower()) {
                case "country":
                    return GlobalPlayerScope.Country;
                case "region":
                    return GlobalPlayerScope.Region;
                default:
                    Plugin.Log.Error("Invalid location filter mode, falling back to country");
                    return GlobalPlayerScope.Country;
            }
        }

        private ClickableImage ScopeImage(GlobalPlayerScope scope) => scope switch {
            GlobalPlayerScope.AroundPlayer => _playerScopeImage,
            GlobalPlayerScope.Friends => _friendsScopeImage,
            GlobalPlayerScope.Country => _countryScopeImage,
            GlobalPlayerScope.Region => _countryScopeImage,
            _ => _globalScopeImage
        };

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling) {
            CancelRefresh();
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        protected override void OnDestroy() {
            CancelRefresh();
            base.OnDestroy();
        }

        private void CancelRefresh() {
            _refreshCancellation?.Cancel();
            _refreshCancellation?.Dispose();
            _refreshCancellation = null;
        }
    }
}
