using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace ScoreSaber.Core.Presentation {

    internal class RemoteImageService {
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

                _coroutineStarter.StartCoroutine(GetSprite(url, onSuccess, onFailure, cancellationToken));
            } catch (OperationCanceledException) {
                onFailure?.Invoke("Cancelled");
            } finally {
                MaintainSpriteCache();
            }
        }

        private IEnumerator GetSprite(string url, Action<Sprite> onSuccess, Action<string> onFailure, CancellationToken cancellationToken) {
            var handler = new DownloadHandlerTexture();
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = handler;
            yield return request.SendWebRequest();

            while (!request.isDone) {
                if (cancellationToken.IsCancellationRequested) {
                    onFailure?.Invoke("Cancelled");
                    yield break;
                }
                yield return null;
            }

            if (request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError) {
                onFailure?.Invoke(request.error);
                yield break;
            }

            if (!string.IsNullOrEmpty(request.error)) {
                onFailure?.Invoke(request.error);
                yield break;
            }

            Sprite sprite = Sprite.Create(handler.texture, new Rect(0, 0, handler.texture.width, handler.texture.height), Vector2.one * 0.5f);
            AddSpriteToCache(url, sprite);
            onSuccess?.Invoke(sprite);
        }

        private void MaintainSpriteCache() {
            while (_cachedSprites.Count > MaxSpriteCacheSize) {
                string oldestUrl = _spriteCacheQueue.Dequeue();
                _cachedSprites.Remove(oldestUrl);
            }
        }

        private void AddSpriteToCache(string url, Sprite sprite) {
            if (_cachedSprites.ContainsKey(url)) {
                return;
            }

            _cachedSprites.Add(url, sprite);
            _spriteCacheQueue.Enqueue(url);
        }
    }
}
