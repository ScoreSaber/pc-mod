using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using ScoreSaber.Core.Compat;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.UI.Cells;
using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Live.Compete.UI.ViewControllers.Rooms {
    [HotReload]
    internal class CompeteRoomListViewController : BSMLAutomaticViewController {
        internal event Action RefreshRequested;
        internal event Action<CompeteRoom> RoomSelected;

        [UIComponent("room-list")]
        private readonly CustomCellListTableData _roomList = null;

        [UIValue("rooms")]
        private readonly List<object> _rooms = new List<object>();

        private string _title = "Rooms";
        private string _subtitle = "Rooms you have permission to join";
        private bool _hasRooms;
        private bool _refreshing;

        [UIValue("room-list-title")]
        private string title {
            get => _title;
            set => SetValue(ref _title, value, nameof(title));
        }

        [UIValue("room-list-subtitle")]
        private string subtitle {
            get => _subtitle;
            set => SetValue(ref _subtitle, value, nameof(subtitle));
        }

        [UIValue("rooms-active")]
        private bool roomsActive => hasRooms && !refreshing;

        [UIValue("rooms-refreshing")]
        private bool refreshing {
            get => _refreshing;
            set {
                _refreshing = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(roomsActive));
                NotifyPropertyChanged(nameof(roomsEmpty));
                NotifyPropertyChanged(nameof(canRefresh));
            }
        }

        [UIValue("can-refresh")]
        private bool canRefresh => !refreshing;

        private bool hasRooms {
            get => _hasRooms;
            set {
                _hasRooms = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(roomsActive));
                NotifyPropertyChanged(nameof(roomsEmpty));
            }
        }

        [UIValue("rooms-empty")]
        private bool roomsEmpty => !hasRooms && !refreshing;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            ReloadList();
        }

        internal void SetTournament(CompeteTournament tournament) {
            title = tournament.Name;
            subtitle = "Rooms you have permission to join";
        }

        internal void SetRooms(IEnumerable<CompeteRoom> rooms) {
            _rooms.Clear();
            foreach (CompeteRoom room in rooms) {
                _rooms.Add(new CompeteRoomCell(room));
            }

            hasRooms = _rooms.Count > 0;
            ReloadList();
        }

        internal void SetRefreshing(bool value) {
            refreshing = value;
            ReloadList();
        }

        internal void SetStatus(string value) {
            subtitle = string.IsNullOrEmpty(value) ? "Rooms you have permission to join" : value;
        }

        [UIAction("refresh-rooms")]
        private void RefreshClicked() {
            if (refreshing) {
                return;
            }

            RefreshRequested?.Invoke();
        }

        [UIAction("room-selected")]
        private void SelectRoom(TableView tableView, CompeteRoomCell cell) {
            if (refreshing) {
                return;
            }

            RoomSelected?.Invoke(cell.Room);
        }

        private void ReloadList() {
            if (_roomList == null) {
                return;
            }

            _roomList.SetData(_rooms);
            _roomList.GetTableView().ReloadData();
            _roomList.GetTableView().ClearSelection();
        }

        private void SetValue<T>(ref T field, T value, string propertyName) {
            field = value;
            NotifyPropertyChanged(propertyName);
        }
    }
}
