using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScoreSaber.Features.MainMenu.MainFlow.GlobalLeaderboard {
    internal class GlobalLeaderboardHost : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        [UIComponent("leaderboard")]
        protected readonly CustomCellListTableData _leaderboard = null;

        [UIValue("current-rank-cells")]
        protected readonly List<object> _rankCells = new List<object>();

        [UIValue("global-loading")]
        protected bool globalLoading => !globalSet;

        private bool _globalSet;
        [UIValue("global-set")]
        protected bool globalSet {
            get => _globalSet;
            set {
                _globalSet = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(globalLoading));
            }
        }

        internal void SetLoading(bool loading) {
            globalSet = !loading;
        }

        internal void SetCells(IEnumerable<GlobalCell> cells) {
            _rankCells.Clear();
            _leaderboard.Data.Clear();
            _rankCells.AddRange(cells);

            globalSet = true;
            _leaderboard.TableView.ReloadData();
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
