using BeatSaberMarkupLanguage.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace ScoreSaber.Features.Live.Compete.UI.Cells {
    internal class CompeteListRowCell {
        [UIValue("row-title")]
        protected readonly string _title;

        [UIValue("row-detail")]
        protected readonly string _detail;

        [UIValue("row-status")]
        protected readonly string _status;

        [UIComponent("row-separator")]
        protected readonly Image _separator = null;

        protected CompeteListRowCell(string title, string detail, string status) {
            _title = title;
            _detail = detail;
            _status = status;
        }

        [UIAction("refresh-visuals")]
        protected void RefreshVisuals(bool selected, bool highlighted) {
            if (_separator != null) {
                _separator.color = highlighted || selected ? new Color(1f, 1f, 1f, 0.55f) : new Color(1f, 1f, 1f, 0.14f);
            }
        }
    }
}
