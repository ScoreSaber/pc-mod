using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using ScoreSaber.Core.Compat;
using ScoreSaber.Core.Presentation;
using ScoreSaber.Features.Leaderboards.Adapters.LeaderboardCore;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.UI.ScoreDetails;
using ScoreSaber.Features.Players.Profile;
using ScoreSaber.Features.Leaderboards.UI.Avatars;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Leaderboards.UI {
    internal class ScoreSaberLeaderboardOverlayController {
        private const string OverlayResource = "ScoreSaber.Features.Leaderboards.UI.ScoreSaberLeaderboardOverlayController.bsml";
        private const float AvatarRailBaseX = -24.4f;

        [UIComponent("root")]
        protected readonly RectTransform _root = null;
        [UIComponent("avatar-rail")]
        private readonly RectTransform _avatarRail = null;
        [UIParams]
        private readonly BSMLParserParams _parserParams = null;

        [UIValue("avatars")]
        internal List<LeaderboardAvatarView> Avatars => _avatarHost.Avatars;

        [UIValue("score-detail-view")]
        protected ScoreDetailView scoreDetailView => _modalFlow.ScoreDetailView;

        [UIComponent("profile-detail-view")]
        protected readonly ProfileDetailView _profileDetailView = null;

        private readonly DiContainer _container;
        private readonly ScoreSaberLeaderboardCoreViewController _leaderboardViewController;
        private readonly LeaderboardAvatarHost _avatarHost;
        private readonly LeaderboardModalFlow _modalFlow;
        private IDisposable _hotReload;

        internal bool IsParsed { get; private set; }

        public ScoreSaberLeaderboardOverlayController(
            DiContainer container,
            ScoreSaberLeaderboardCoreViewController leaderboardViewController,
            LeaderboardAvatarHost avatarHost,
            LeaderboardModalFlow modalFlow) {

            _container = container;
            _leaderboardViewController = leaderboardViewController;
            _avatarHost = avatarHost;
            _modalFlow = modalFlow;
        }

        internal void EnsureParsed() {
            if (!IsParsed) {
                Parse();
                _hotReload?.Dispose();
                _hotReload = BSMLHotReload.Watch(null, Reload);
                IsParsed = true;
            }
        }

        private void Reload() {
            if (_root != null) {
                UnityEngine.Object.Destroy(_root.gameObject);
            }

            Parse();
        }

        private void Parse() {
            BsmlCompat.Parser.Parse(BSMLHotReload.ResourceContent(typeof(ScoreSaberLeaderboardOverlayController).Assembly, OverlayResource), _leaderboardViewController.gameObject, this);
            _container.Inject(_profileDetailView);
            _modalFlow.Bind(_parserParams, _profileDetailView);
        }

        internal void ApplyAvatarLayout(LeaderboardMap leaderboard) {
            if (_avatarRail == null || leaderboard == null) {
                return;
            }

            _avatarRail.anchoredPosition = new Vector2(AvatarRailBaseX + LeaderboardRankLayout.OffsetFor(leaderboard), _avatarRail.anchoredPosition.y);
        }

        [UIAction("#post-parse")]
        public void Parsed() {
            _root.name = "ScoreSaberLeaderboardElements";
        }
    }

    internal static class LeaderboardRankLayout {
        private const int BaselineRankDigits = 2;
        private const float RankDigitOffset = 1.15f;

        internal static float OffsetFor(LeaderboardMap leaderboard) {
            if (leaderboard == null || leaderboard.Scores == null) {
                return 0f;
            }

            int rankDigits = BaselineRankDigits;
            for (int i = 0; i < leaderboard.Scores.Length; i++) {
                int rank = leaderboard.Scores[i].Score.Rank;
                int digits = rank <= 0 ? 1 : rank.ToString().Length;
                if (digits > rankDigits) {
                    rankDigits = digits;
                }
            }

            return Mathf.Max(0, rankDigits - BaselineRankDigits) * RankDigitOffset;
        }
    }
}
