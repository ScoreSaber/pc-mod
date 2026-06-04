using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using ScoreSaber.Core.Presentation;
using System;
using System.Threading;
using UnityEngine;

namespace ScoreSaber.Features.Leaderboards.UI.Avatars {
    internal class LeaderboardAvatarView {
        private readonly RemoteImageService _remoteImageService;
        private readonly ScoreSaberUIMaterials _materials;
        private readonly Sprite _blankSprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;

        public LeaderboardAvatarView(RemoteImageService remoteImageService, ScoreSaberUIMaterials materials) {
            _remoteImageService = remoteImageService;
            _materials = materials;
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
        }

        private void ClearIfActive(CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            Clear();
        }
    }
}
