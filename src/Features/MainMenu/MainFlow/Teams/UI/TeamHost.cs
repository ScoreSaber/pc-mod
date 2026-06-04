using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using ScoreSaber.Core.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ScoreSaber.Features.MainMenu.MainFlow.Teams.UI {
    internal class TeamHost {
        private const string TeamHostResource = "ScoreSaber.Features.MainMenu.MainFlow.Teams.UI.TeamHost.bsml";

        [UIComponent("tab-root")]
        protected readonly RectTransform _tabRoot = null;

        [UIComponent("grid")]
        protected readonly GridLayoutGroup _grid = null;

        [UIValue("profiles")]
        public List<object> profiles = new List<object>();

        [UIValue("needs-scroll-view")]
        protected bool needsScrollView => profiles.Count > 9;

        [UIValue("team-name")]
        public string _teamName { get; }

        private bool _parsed;
        private IDisposable _hotReload;
        private GameObject _parentGrid;

        public TeamHost(string teamName, IEnumerable<TeamUserInfo> profiles) {
            _teamName = teamName;
            this.profiles = profiles.Cast<object>().ToList();
        }

        public void Init() {
            if (_tabRoot != null) {
                Parse(_tabRoot.gameObject);
            } else {
                Plugin.Log.Info("tabRoot is null");
            }
        }

        public void Parse(GameObject parentGrid) {
            if (!_parsed) {
                _parentGrid = parentGrid;
                ParseContent();
                _hotReload?.Dispose();
                _hotReload = BSMLHotReload.Watch(null, Reload);
                _parsed = true;
            }
        }

        private void Reload() {
            if (_parentGrid == null) {
                return;
            }

            BSMLHotReload.ClearChildren(_parentGrid.transform);
            ParseContent();
        }

        private void ParseContent() {
            BSMLParser.Instance.Parse(BSMLHotReload.ResourceContent(typeof(TeamHost).Assembly, TeamHostResource), _parentGrid, this);
            if (_grid != null) {
                _grid.constraintCount = 3;
                _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            }
            foreach (TeamUserInfo profile in profiles.Cast<TeamUserInfo>()) {
                profile.LoadImage();
            }
        }
    }
}
