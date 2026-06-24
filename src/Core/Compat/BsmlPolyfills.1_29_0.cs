using System.Threading.Tasks;
using UnityEngine.UI;

namespace ScoreSaber {
    internal static class BsmlPolyfills {
        public static Task SetImageAsync(this Image image, string location) {
            BeatSaberMarkupLanguage.BeatSaberUI.SetImage(image, location);
            return Task.CompletedTask;
        }
    }
}
