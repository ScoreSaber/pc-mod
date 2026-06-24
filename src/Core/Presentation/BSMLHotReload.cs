using BeatSaberMarkupLanguage;
using IPA.Utilities.Async;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace ScoreSaber.Core.Presentation {
    internal static class BSMLHotReload {
        internal static string ResourceContent(Assembly assembly, string resource, string relativePathToLayout = null, [CallerFilePath] string sourcePath = null) {
#if DEBUG || USE_HOT_RELOAD
            string filePath = LayoutPath(relativePathToLayout, sourcePath);
            if (File.Exists(filePath)) {
                return File.ReadAllText(filePath);
            }
#endif

            return Utilities.GetResourceContent(assembly, resource);
        }

        internal static IDisposable Watch(string relativePathToLayout, Action reload, [CallerFilePath] string sourcePath = null) {
#if DEBUG || USE_HOT_RELOAD
            string filePath = LayoutPath(relativePathToLayout, sourcePath);
            if (reload != null && File.Exists(filePath)) {
                return new LayoutWatcher(filePath, reload);
            }
#endif

            return null;
        }

        internal static void ClearChildren(Transform transform) {
            for (int i = transform.childCount - 1; i >= 0; i--) {
                UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

#if DEBUG || USE_HOT_RELOAD
        private static string LayoutPath(string relativePathToLayout, string sourcePath) {
            string path = string.IsNullOrEmpty(relativePathToLayout)
                ? Path.ChangeExtension(sourcePath, ".bsml")
                : Path.Combine(Path.GetDirectoryName(sourcePath), relativePathToLayout);

            return Path.GetFullPath(path);
        }

        private sealed class LayoutWatcher : IDisposable {
            private static readonly TimeSpan ReloadDelay = TimeSpan.FromSeconds(0.5f);

            private readonly FileSystemWatcher _watcher;
            private readonly Action _reload;
            private bool _isReloading;

            internal LayoutWatcher(string filePath, Action reload) {
                _reload = reload;
                _watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath), Path.GetFileName(filePath)) {
                    NotifyFilter = NotifyFilters.LastWrite,
                    EnableRaisingEvents = true,
                };
                _watcher.Changed += FileChanged;
            }

            public void Dispose() {
                _watcher.Changed -= FileChanged;
                _watcher.Dispose();
            }

            private async void FileChanged(object sender, FileSystemEventArgs args) {
                if (_isReloading) {
                    return;
                }

                _isReloading = true;
                await Task.Delay(ReloadDelay);
                await UnityMainThreadTaskScheduler.Factory.StartNew(_reload);
                _isReloading = false;
            }
        }
#endif
    }
}
