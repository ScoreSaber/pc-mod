using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using ScoreSaber.Core.Compat;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.UI.Cells;
using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Live.UI.ViewControllers {
    [HotReload]
    internal class TournamentBrowserViewController : BSMLAutomaticViewController {
        internal event Action RefreshRequested;
        internal event Action<CompeteTournament> TournamentSelected;

        [UIComponent("tournament-list")]
        private readonly CustomCellListTableData _tournamentList = null;

        [UIValue("tournaments")]
        private readonly List<object> _tournaments = new List<object>();

        private bool _hasTournaments;

        [UIValue("has-tournaments")]
        private bool hasTournaments {
            get => _hasTournaments;
            set {
                _hasTournaments = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(tournamentsEmpty));
            }
        }

        [UIValue("tournaments-empty")]
        private bool tournamentsEmpty => !hasTournaments;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            ReloadList();
        }

        internal void SetTournaments(IEnumerable<CompeteTournament> tournaments) {
            _tournaments.Clear();
            foreach (CompeteTournament tournament in tournaments) {
                _tournaments.Add(new CompeteTournamentCell(tournament));
            }

            hasTournaments = _tournaments.Count > 0;
            ReloadList();
        }

        [UIAction("refresh-tournaments")]
        private void RefreshClicked() {
            RefreshRequested?.Invoke();
        }

        [UIAction("tournament-selected")]
        private void SelectTournament(TableView tableView, CompeteTournamentCell cell) {
            TournamentSelected?.Invoke(cell.Tournament);
        }

        private void ReloadList() {
            if (_tournamentList == null) {
                return;
            }

            _tournamentList.SetData(_tournaments);
            _tournamentList.GetTableView().ReloadData();
            _tournamentList.GetTableView().ClearSelection();
        }
    }
}
