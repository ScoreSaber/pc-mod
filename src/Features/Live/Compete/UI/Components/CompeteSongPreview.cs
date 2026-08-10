using HMUI;
using IPA.Utilities.Async;
using ScoreSaber.Core;
using ScoreSaber.Features.Live.Compete.Domain;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScoreSaber.Features.Live.Compete.UI.Components {
    internal class CompeteSongPreview {
        private const float MinSongTextWidth = 18f;
        private const float MaxSongTextWidth = 38f;

        private ImageView _coverImage;
        private LayoutElement _textColumnLayout;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _detailText;
        private TextMeshProUGUI _difficultyText;
        private RectTransform _contentTransform;
        private CompeteSongSelection _song;
        private int _coverRequestVersion;

        internal string Name { get; private set; } = string.Empty;
        internal string Detail { get; private set; } = string.Empty;
        internal string Difficulty { get; private set; } = string.Empty;
        internal string Duration { get; private set; } = "--";
        internal string Bpm { get; private set; } = "--";
        internal string Nps { get; private set; } = "--";
        internal string Notes { get; private set; } = "--";
        internal string Obstacles { get; private set; } = "--";
        internal string Bombs { get; private set; } = "--";
        internal string Njs { get; private set; } = "--";
        internal string JumpDistance { get; private set; } = "--";
        internal string Stars { get; private set; } = "--";
        internal bool IsEmpty { get; private set; } = true;
        internal bool IsActive => !IsEmpty;

        internal void Bind(
            ImageView coverImage,
            LayoutElement textColumnLayout,
            TextMeshProUGUI nameText,
            TextMeshProUGUI detailText,
            TextMeshProUGUI difficultyText,
            RectTransform contentTransform) {

            _coverImage = coverImage;
            _textColumnLayout = textColumnLayout;
            _nameText = nameText;
            _detailText = detailText;
            _difficultyText = difficultyText;
            _contentTransform = contentTransform;
        }

        internal void SetSong(CompeteSongSelection song) {
            _song = song;
            IsEmpty = song == null;
            Name = song == null ? string.Empty : song.Name;
            Detail = song == null ? string.Empty : $"Mapped by {song.Mapper}";
            Difficulty = song == null ? string.Empty : $"{song.Difficulty} / {song.Characteristic}";
            Duration = song == null ? "--" : song.Duration;
            Bpm = song == null ? "--" : song.Bpm;
            Nps = song == null ? "--" : song.Nps;
            Notes = song == null ? "--" : song.Notes;
            Obstacles = song == null ? "--" : song.Obstacles;
            Bombs = song == null ? "--" : song.Bombs;
            Njs = song == null ? "--" : song.Njs;
            JumpDistance = song == null ? "--" : song.JumpDistance;
            Stars = song == null ? "--" : song.Stars;
        }

        internal void RefreshVisuals() {
            UpdateLayout();
            LoadCover(_song).RunTask();
        }

        private async Task LoadCover(CompeteSongSelection song) {
            int requestVersion = ++_coverRequestVersion;
            if (_coverImage == null) {
                return;
            }

            if (song == null || song.BeatmapLevel == null) {
                _coverImage.sprite = null;
                _coverImage.color = Color.clear;
                return;
            }

            try {
                Sprite cover = await song.BeatmapLevel.previewMediaData.GetCoverSpriteAsync();
                await UnityMainThreadTaskScheduler.Factory.StartNew(() => {
                    if (requestVersion != _coverRequestVersion || _coverImage == null) {
                        return;
                    }

                    _coverImage.sprite = cover;
                    _coverImage.color = cover == null ? Color.clear : Color.white;
                });
            } catch (Exception ex) {
                Plugin.Log.Error($"Failed to load mock compete song cover: {ex}");
            }
        }

        private void UpdateLayout() {
            if (_textColumnLayout == null) {
                return;
            }

            float textWidth = Mathf.Max(
                PreferredWidth(_nameText, Name),
                PreferredWidth(_detailText, Detail),
                PreferredWidth(_difficultyText, Difficulty));

            float width = Mathf.Clamp(textWidth, MinSongTextWidth, MaxSongTextWidth);
            _textColumnLayout.preferredWidth = width;
            _textColumnLayout.minWidth = width;

            if (_contentTransform != null) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentTransform);
            }
        }

        private static float PreferredWidth(TextMeshProUGUI text, string value) {
            if (text == null || string.IsNullOrEmpty(value)) {
                return MinSongTextWidth;
            }

            text.ForceMeshUpdate();
            return text.GetPreferredValues(value, float.PositiveInfinity, 0f).x;
        }
    }
}
