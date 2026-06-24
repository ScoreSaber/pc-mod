using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using ScoreSaber.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScoreSaber.Features.Players.Profile {
    internal class BadgeCell : INotifyPropertyChanged {

        [UIComponent("image")]
        protected readonly ImageView _image = null;

        private string _hoverHint = "";
        [UIValue("hover-hint")]
        protected string hoverHint {
            get => _hoverHint;
            set {
                _hoverHint = value;
                NotifyPropertyChanged();
            }
        }

        public void SetData(string imageURL, string hoverHintText) {

            _image.SetImageAsync(imageURL).RunTask();
            hoverHint = hoverHintText;
        }

        public void SetActive(bool value) {

            _image.gameObject.SetActive(value);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
