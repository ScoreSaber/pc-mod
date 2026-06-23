using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using ScoreSaber.Core;
using ScoreSaber.Core.Compat;
using ScoreSaber.Core.Presentation;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.UI.Cells;
using ScoreSaber.Features.Players.Profile;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Live.Compete.UI.ViewControllers.Room.Left {
    [HotReload]
    internal class CompetePlayerListViewController : BSMLAutomaticViewController {
        private const float ScrollbarWidth = 8f;
        private static readonly CompeteTeam FallbackTeamOne = new CompeteTeam("team1", "Team 1");
        private static readonly CompeteTeam FallbackTeamTwo = new CompeteTeam("team2", "Team 2");

        [UIComponent("player-list")]
        private readonly CustomCellListTableData _playerList = null;

        [UIComponent("team-one-player-list")]
        private readonly CustomCellListTableData _teamOnePlayerList = null;

        [UIComponent("team-two-player-list")]
        private readonly CustomCellListTableData _teamTwoPlayerList = null;

        [UIComponent("profile-modal")]
        private readonly ProfileDetailView _profileDetailView = null;

        [UIParams]
        private readonly BSMLParserParams _parserParams = null;

        [UIValue("players")]
        private readonly List<object> _players = new List<object>();

        [UIValue("team-one-players")]
        private readonly List<object> _teamOnePlayers = new List<object>();

        [UIValue("team-two-players")]
        private readonly List<object> _teamTwoPlayers = new List<object>();

        private DiContainer _container;
        private ScoreSaberUIMaterials _materials;
        private CompeteTeam _teamOne = FallbackTeamOne;
        private CompeteTeam _teamTwo = FallbackTeamTwo;
        private bool _hasPlayers;
        private bool _teamMode;

        [Inject]
        private void Construct(DiContainer container, ScoreSaberUIMaterials materials) {
            _container = container;
            _materials = materials;
        }

        [UIValue("has-players")]
        private bool hasPlayers {
            get => _hasPlayers;
            set {
                _hasPlayers = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(playersEmpty));
                NotifyPropertyChanged(nameof(regularPlayersVisible));
                NotifyPropertyChanged(nameof(teamPlayersVisible));
            }
        }

        [UIValue("players-empty")]
        private bool playersEmpty => !hasPlayers;

        [UIValue("regular-players-visible")]
        private bool regularPlayersVisible => hasPlayers && !_teamMode;

        [UIValue("team-players-visible")]
        private bool teamPlayersVisible => hasPlayers && _teamMode;

        [UIValue("team-one-name")]
        private string teamOneName => _teamOne.Name;

        [UIValue("team-two-name")]
        private string teamTwoName => _teamTwo.Name;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            ReloadPlayers();
            MoveTeamOneScrollbarToLeft();
        }

        [UIAction("#post-parse")]
        private void Parsed() {
            if (_profileDetailView != null) {
                _container.Inject(_profileDetailView);
            }
        }

        internal void SetRoom(CompeteRoom room) {
            _players.Clear();
            _teamOnePlayers.Clear();
            _teamTwoPlayers.Clear();

            _teamMode = room.PlayerListMode == CompetePlayerListMode.Teams;
            _teamOne = room.Teams.Count > 0 ? room.Teams[0] : FallbackTeamOne;
            _teamTwo = room.Teams.Count > 1 ? room.Teams[1] : FallbackTeamTwo;
            foreach (CompetePlayer player in room.Players) {
                var cell = new CompetePlayerCell(player, _materials, ShowProfile);
                if (_teamMode) {
                    (player.TeamId == _teamOne.Id ? _teamOnePlayers : _teamTwoPlayers).Add(cell);
                    continue;
                }

                _players.Add(cell);
            }

            hasPlayers = room.Players.Count > 0;
            NotifyPropertyChanged(nameof(regularPlayersVisible));
            NotifyPropertyChanged(nameof(teamPlayersVisible));
            NotifyPropertyChanged(nameof(teamOneName));
            NotifyPropertyChanged(nameof(teamTwoName));
            ReloadPlayers();
        }

        private void ReloadPlayers() {
            ReloadList(_playerList, _players);
            ReloadList(_teamOnePlayerList, _teamOnePlayers);
            ReloadList(_teamTwoPlayerList, _teamTwoPlayers);
            MoveTeamOneScrollbarToLeft();
        }

        private static void ReloadList(CustomCellListTableData list, List<object> players) {
            if (list == null) {
                return;
            }

            list.SetData(players);
            list.GetTableView().ReloadData();
            list.GetTableView().ClearSelection();
        }

        private void MoveTeamOneScrollbarToLeft() {
            if (_teamOnePlayerList == null) {
                return;
            }

            VerticalScrollIndicator indicator = _teamOnePlayerList.GetComponentInChildren<VerticalScrollIndicator>(true);
            RectTransform scrollbar = indicator == null ? null : indicator.transform.parent as RectTransform;
            if (scrollbar == null) {
                return;
            }

            scrollbar.anchorMin = new Vector2(0f, 0f);
            scrollbar.anchorMax = new Vector2(0f, 1f);
            scrollbar.offsetMin = new Vector2(-ScrollbarWidth, 0f);
            scrollbar.offsetMax = Vector2.zero;
        }

        private void ShowProfile(string playerId, string name) {
            ShowProfileAsync(playerId, name).RunTask();
        }

        private async Task ShowProfileAsync(string playerId, string name) {
            if (_profileDetailView == null || _parserParams == null || string.IsNullOrEmpty(playerId)) {
                return;
            }

            _parserParams.EmitEvent("close-modals");
            _parserParams.EmitEvent("show-profile");
            _profileDetailView.SetLoadingState(true);
            if (_profileDetailView.playerNameText != null && !string.IsNullOrEmpty(name)) {
                _profileDetailView.playerNameText.text = name;
            }

            try {
                await _profileDetailView.ShowProfile(playerId);
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to show live room player profile: {ex.Message}");
                _profileDetailView.SetLoadingState(false);
            }
        }

    }
}
