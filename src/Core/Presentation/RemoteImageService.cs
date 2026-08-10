using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace ScoreSaber.Core.Presentation {

    internal class RemoteImageService : IDisposable {
        private const int MaxSpriteCacheSize = 150;

        private readonly Dictionary<string, Sprite> _cachedSprites = new Dictionary<string, Sprite>();
        private readonly Queue<string> _spriteCacheQueue = new Queue<string>();
        private ICoroutineStarter _coroutineStarter;

        [Inject]
        public void Init(ICoroutineStarter coroutineStarter) {
            _coroutineStarter = coroutineStarter;
        }

        internal void LoadSprite(string url, Action<Sprite> onSuccess, Action<string> onFailure, CancellationToken cancellationToken) {
            try {
                cancellationToken.ThrowIfCancellationRequested();
                if (_cachedSprites.TryGetValue(url, out Sprite sprite)) {
                    onSuccess?.Invoke(sprite);
                    return;
                }

                if (_coroutineStarter == null) {
                    onFailure?.Invoke("Coroutine starter unavailable");
                    return;
                }

                _coroutineStarter.StartCoroutine(GetSprite(url, onSuccess, onFailure, cancellationToken));
            } catch (OperationCanceledException) {
                onFailure?.Invoke("Cancelled");
            } finally {
                MaintainSpriteCache();
            }
        }

        private IEnumerator GetSprite(string url, Action<Sprite> onSuccess, Action<string> onFailure, CancellationToken cancellationToken) {
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)) {
                var handler = new DownloadHandlerTexture();
                request.downloadHandler = handler;
                request.disposeDownloadHandlerOnDispose = true;

                AsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone) {
                    if (cancellationToken.IsCancellationRequested) {
                        request.Abort();
                        onFailure?.Invoke("Cancelled");
                        yield break;
                    }

                    yield return null;
                }

                if (cancellationToken.IsCancellationRequested) {
                    onFailure?.Invoke("Cancelled");
                    yield break;
                }

                if (request.IsProtocolError() || request.IsConnectionError()) {
                    onFailure?.Invoke(request.error);
                    yield break;
                }

                if (!string.IsNullOrEmpty(request.error)) {
                    onFailure?.Invoke(request.error);
                    yield break;
                }

                Texture2D texture = handler.texture;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                AddSpriteToCache(url, sprite);
                onSuccess?.Invoke(sprite);
            }
        }

        private void MaintainSpriteCache() {
            while (_cachedSprites.Count > MaxSpriteCacheSize) {
                string oldestUrl = _spriteCacheQueue.Dequeue();
                if (_cachedSprites.TryGetValue(oldestUrl, out Sprite sprite)) {
                    _cachedSprites.Remove(oldestUrl);
                    DestroySprite(sprite);
                }
            }
        }

        private void AddSpriteToCache(string url, Sprite sprite) {
            if (_cachedSprites.ContainsKey(url)) {
                DestroySprite(sprite);
                return;
            }

            _cachedSprites.Add(url, sprite);
            _spriteCacheQueue.Enqueue(url);
            MaintainSpriteCache();
        }

        public void Dispose() {
            foreach (Sprite sprite in _cachedSprites.Values) {
                DestroySprite(sprite);
            }

            _cachedSprites.Clear();
            _spriteCacheQueue.Clear();
        }

        private static void DestroySprite(Sprite sprite) {
            if (sprite == null) {
                return;
            }

            Texture2D texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);
            if (texture != null) {
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
