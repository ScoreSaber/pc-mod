using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Core.Presentation;
using HMUI;
using BeatSaberMarkupLanguage.Attributes;
using System;

namespace ScoreSaber.Features.Live.Compete.UI.Cells {
    internal class CompetePlayerCell : CompeteListRowCell {
        private const string DefaultAvatar = "ScoreSaber.Resources.user.png";

        private readonly CompetePlayer _player;
        private readonly Action<string, string> _profileClicked;
        private readonly ScoreSaberUIMaterials _materials;

        [UIValue("avatar-url")]
        private string avatarUrl => string.IsNullOrEmpty(_player.AvatarUrl) ? DefaultAvatar : _player.AvatarUrl;

        [UIComponent("profile-image")]
        private readonly ImageView _profileImage = null;

        internal CompetePlayerCell(CompetePlayer player, ScoreSaberUIMaterials materials, Action<string, string> profileClicked)
            : base(
                player.IsLocalPlayer ? $"{player.DisplayName} (you)" : player.DisplayName,
                player.Rank,
                player.Status) {

            _player = player;
            _materials = materials;
            _profileClicked = profileClicked;
        }

        [UIAction("profile-clicked")]
        private void ProfileClicked() {
            if (string.IsNullOrEmpty(_player.PlayerId) || _player.IsBot) {
                return;
            }

            _profileClicked?.Invoke(_player.PlayerId, _player.DisplayName);
        }

        [UIAction("#post-parse")]
        private void Parsed() {
            if (_profileImage != null && _materials != null) {
                _profileImage.material = _materials.RoundedImageMaterial;
            }
        }
    }
}
