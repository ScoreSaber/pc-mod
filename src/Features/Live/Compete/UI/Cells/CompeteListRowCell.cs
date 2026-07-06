using BeatSaberMarkupLanguage.Attributes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace ScoreSaber.Features.Live.Compete.UI.Cells {
    internal class CompeteListRowCell : INotifyPropertyChanged {
        private string _title;
        private string _detail;
        private string _status;

        [UIValue("row-title")]
        protected string rowTitle {
            get => _title;
            set => SetValue(ref _title, value, nameof(rowTitle));
        }

        [UIValue("row-detail")]
        protected string rowDetail {
            get => _detail;
            set => SetValue(ref _detail, value, nameof(rowDetail));
        }

        [UIValue("row-status")]
        protected string rowStatus {
            get => _status;
            set => SetValue(ref _status, value, nameof(rowStatus));
        }

        [UIComponent("row-separator")]
        protected readonly Image _separator = null;

        protected CompeteListRowCell(string title, string detail, string status) {
            _title = title ?? string.Empty;
            _detail = detail ?? string.Empty;
            _status = status ?? string.Empty;
        }

        [UIAction("refresh-visuals")]
        protected void RefreshVisuals(bool selected, bool highlighted) {
            if (_separator != null) {
                _separator.color = highlighted || selected ? new Color(1f, 1f, 1f, 0.55f) : new Color(1f, 1f, 1f, 0.14f);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetValue(ref string field, string value, string propertyName) {
            value = value ?? string.Empty;
            if (field == value) {
                return;
            }

            field = value;
            NotifyPropertyChanged(propertyName);
        }
    }
}
