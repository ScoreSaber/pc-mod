using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace ScoreSaber.Features.Live.Compete.UI.ViewControllers.Shared {
    [HotReload]
    internal class CompeteLoadingViewController : BSMLAutomaticViewController {
        private string _message = "Loading...";

        [UIValue("loading-message")]
        private string loadingMessage {
            get => _message;
            set {
                _message = value;
                NotifyPropertyChanged();
            }
        }

        internal void SetMessage(string message) {
            loadingMessage = message.EndsWith("...") ? message : $"{message}...";
        }
    }
}
