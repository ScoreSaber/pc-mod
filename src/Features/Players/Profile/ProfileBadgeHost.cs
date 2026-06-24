using BeatSaberMarkupLanguage.Attributes;
using System.Collections.Generic;
using UnityEngine.UI;

namespace ScoreSaber.Features.Players.Profile {
    internal class ProfileBadgeHost {
        private const int BadgeCellCount = 12;

        [UIComponent("badge-grid")]
        private readonly GridLayoutGroup _badgeGrid = null;

        [UIValue("badge-host-list")]
        protected readonly List<object> badgeList = new List<object>();

        public ProfileBadgeHost() {
            for (int i = 0; i < BadgeCellCount; i++) {
                badgeList.Add(new BadgeCell());
            }
        }

        internal void SetBadges(IReadOnlyList<ProfileBadgeData> badges) {
            if (badges == null || badges.Count == 0) {
                SetGridActive(false);
                return;
            }

            SetGridActive(true);
            int count = badges.Count < badgeList.Count ? badges.Count : badgeList.Count;
            for (int i = 0; i < count; i++) {
                var cell = badgeList[i] as BadgeCell;
                cell.SetData(badges[i].Image, badges[i].Description);
                cell.SetActive(true);
            }

            for (int i = count; i < badgeList.Count; i++) {
                (badgeList[i] as BadgeCell).SetActive(false);
            }
        }

        private void SetGridActive(bool active) {
            if (_badgeGrid != null) {
                _badgeGrid.gameObject.SetActive(active);
            }
        }
    }
}
