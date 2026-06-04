using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using ScoreSaber.Core.Presentation;
using ScoreSaber.Core;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ScoreSaber.Features.MainMenu.MainFlow.Teams.UI {
    internal class TeamUserInfo : INotifyPropertyChanged {

        private string _usernameText = null;
        [UIValue("username")]
        protected string usernameText {
            get => _usernameText;
            set {
                _usernameText = value;
                NotifyPropertyChanged();
            }
        }

        private string _discordLink = null;
        protected string discordLink {
            get => _discordLink;
            set => SetLink(ref _discordLink, value, string.Empty, "hasDiscord");
        }

        private string _githubLink = null;
        protected string githubLink {
            get => _githubLink;
            set => SetLink(ref _githubLink, value, "https://github.com/", "hasGithub");
        }

        private string _twitchLink = null;
        protected string twitchLink {
            get => _twitchLink;
            set => SetLink(ref _twitchLink, value, "https://www.twitch.tv/", "hasTwitch");
        }

        private string _twitterLink = null;
        protected string twitterLink {
            get => _twitterLink;
            set => SetLink(ref _twitterLink, value, "https://twitter.com/", "hasTwitter");
        }

        private string _youtubeLink = null;
        protected string youtubeLink {
            get => _youtubeLink;
            set => SetLink(ref _youtubeLink, value, "https://www.youtube.com/channel/", "hasYoutube");
        }

        private readonly string _profilePictureTemp;
        private readonly ScoreSaberUIMaterials _materials;
        private bool _loaded;

        [UIValue("discord")]
        private bool _hasDiscord => _discordLink != null;
        [UIValue("github")]
        private bool _hasGithub => _githubLink != null;
        [UIValue("twitch")]
        private bool _hasTwitch => _twitchLink != null;
        [UIValue("twitter")]
        private bool _hasTwitter => _twitterLink != null;
        [UIValue("youtube")]
        private bool _hasYoutube => _youtubeLink != null;

        [UIComponent("username-text")]
        protected readonly CurvedTextMeshPro _usernameTextComponent = null;

        [UIComponent("profile-image")]
        protected readonly ImageView _profilePictureComponent = null;

        public TeamUserInfo(ScoreSaberUIMaterials materials, string _profilePicture, string _username, string _discord = null, string _github = null, string _twitch = null, string _twitter = null, string _youtube = null) {

            if (_username == "williums") {
                _username = "<color=#FF0000>w</color><color=#FF7F00>i</color><color=#FFFF00>l</color><color=#00FF00>l</color><color=#0000FF>i</color><color=#4B0082>u</color><color=#8B00FF>m</color><color=#FF0000>s</color>";
            }

            _materials = materials;
            _profilePictureTemp = _profilePicture;
            usernameText = _username;
            discordLink = _discord;
            githubLink = _github;
            twitchLink = _twitch;
            twitterLink = _twitter;
            youtubeLink = _youtube;
        }

        public void LoadImage() {

            if (_loaded) {
                return;
            }

            if (_profilePictureTemp != null) {
                SetImage(_profilePictureTemp);
            }
            _loaded = true;
        }

        private void SetImage(string image) {

            if (_profilePictureComponent != null) {
                _profilePictureComponent.SetImageAsync($"https://raw.githubusercontent.com/Umbranoxio/ScoreSaber-Team/main/images/{image}").RunTask();
            } else {
                Plugin.Log.Info("ProfilePictureComponent is null");
            }
        }

        public int clickCounter = 0;
        [UIAction("username-click")]
        public void UsernameClick() {
            if (usernameText != "Umbranox") {
                return;
            }

            if (clickCounter < 5) {
                clickCounter++;
            }
            if (clickCounter != 5) {
                return;
            }

            SetImage("r.jpg");
            usernameText = "🌧 Rain ❤";
            discordLink = "128460955272216576";
            twitterLink = "VaporRain";
            twitchLink = "inkierain";
            NotifyPropertyChanged("profilePicture");
            youtubeLink = null;
            githubLink = null;
        }

        [UIAction("#post-parse")]
        protected void Parsed() {

            _profilePictureComponent.material = _materials.RoundedImageMaterial;
            _usernameTextComponent.fontSizeMax = 5.5f;
            _usernameTextComponent.fontSizeMin = 2.5f;
            _usernameTextComponent.enableAutoSizing = true;
        }

        [UIAction("discord-clicked")]
        protected void DiscordClicked() => Application.OpenURL(_discordLink);

        [UIAction("github-clicked")]
        protected void GitHubClicked() => Application.OpenURL(_githubLink);

        [UIAction("twitter-clicked")]
        protected void TwitchClicked() => Application.OpenURL(_twitchLink);

        [UIAction("twitch-clicked")]
        protected void TwitterClicked() => Application.OpenURL(_twitterLink);


        [UIAction("youtube-clicked")]
        protected void YoutubeClicked() => Application.OpenURL(_youtubeLink);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void SetLink(ref string field, string value, string prefix, string propertyName) {
            field = value == null ? null : prefix + value;
            NotifyPropertyChanged(propertyName);
        }
    }
}
