using IPA.Utilities.Async;
using ScoreSaber.Features.Live.Compete.Domain;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScoreSaber.Features.Live.Ludus.Services {
    internal sealed class LiveChatSongNavigator {
        internal Task<bool> TryFocusSong(CompeteSongSelection selection, CancellationToken cancellationToken) {
            if (selection?.BeatmapLevel == null) {
                return Task.FromResult(false);
            }

            return UnityMainThreadTaskScheduler.Factory.StartNew(() => TryFocusSongOnMainThread(selection), cancellationToken);
        }

        private static bool TryFocusSongOnMainThread(CompeteSongSelection selection) {
            bool focused = false;

            foreach (LevelCollectionNavigationController controller in Active<LevelCollectionNavigationController>()) {
#if BEAT_SABER_1_29_0
                controller.SelectLevel(selection.BeatmapLevel.previewBeatmapLevel);
#else
                controller.SelectLevel(selection.BeatmapLevel);
#endif
                focused = true;
            }

            foreach (LevelCollectionViewController controller in Active<LevelCollectionViewController>()) {
#if BEAT_SABER_1_29_0
                controller.SelectLevel(selection.BeatmapLevel.previewBeatmapLevel);
#else
                controller.SelectLevel(selection.BeatmapLevel);
#endif
                focused = true;
            }

            if (focused) {
                Plugin.Log.Info($"Live chat selected linked map: {selection.Name}");
            }

            return focused;
        }

        private static T[] Active<T>() where T : Component {
            return Resources.FindObjectsOfTypeAll<T>()
                .Where(item => item != null && item.gameObject != null && item.gameObject.activeInHierarchy)
                .ToArray();
        }
    }
}
