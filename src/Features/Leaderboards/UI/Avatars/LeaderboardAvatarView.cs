using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using ScoreSaber.Core.Presentation;
using ScoreSaber.Features.Leaderboards.Services;
using System;
using System.Threading;
using UnityEngine;

namespace ScoreSaber.Features.Leaderboards.UI.Avatars {
    internal class LeaderboardAvatarView {
        private int _index;

        private readonly RemoteImageService _remoteImageService;
        private readonly ScoreSaberUIMaterials _materials;
        private readonly LeaderboardTweeningService _leaderboardTweeningService;
        private readonly Sprite _blankSprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;

        public LeaderboardAvatarView(int index, RemoteImageService remoteImageService, ScoreSaberUIMaterials materials, LeaderboardTweeningService leaderboardTweeningService) {
            _index = index;
            _remoteImageService = remoteImageService;
            _materials = materials;
            _leaderboardTweeningService = leaderboardTweeningService;
        }

        [UIComponent("profileImage")]
        public ImageView ProfileImage = null;

        [UIObject("profileloading")]
        public GameObject LoadingIndicator = null;

        [UIAction("#post-parse")]
        public void Parsed() {
            ProfileImage.material = _materials.RoundedImageMaterial;
            ProfileImage.sprite = _blankSprite;
            ProfileImage.gameObject.SetActive(true);
            LoadingIndicator.gameObject.SetActive(false);
        }

        internal void Load(string url, CancellationToken cancellationToken) {
            try {
                cancellationToken.ThrowIfCancellationRequested();
                LoadingIndicator.gameObject.SetActive(true);
                _remoteImageService.LoadSprite(
                    url,
                    sprite => SetSprite(sprite, cancellationToken),
                    _ => ClearIfActive(cancellationToken),
                    cancellationToken);
            } catch (OperationCanceledException) {
                ClearIfActive(cancellationToken);
            }
        }

        internal void Clear() {
            if (ProfileImage != null) {
                ProfileImage.sprite = _blankSprite;
            }
            if (LoadingIndicator != null) {
                LoadingIndicator.gameObject.SetActive(false);
            }
        }

        private void SetSprite(Sprite sprite, CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            ProfileImage.gameObject.SetActive(true);
            ProfileImage.sprite = sprite;
            LoadingIndicator.gameObject.SetActive(false);
            _leaderboardTweeningService.CreateImageViewFade("avatar " + _index, 0f, 1f, 0.5f, ProfileImage);
        }

        private void ClearIfActive(CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            Clear();
        }
    }
}
