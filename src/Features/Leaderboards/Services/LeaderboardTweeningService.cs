using HMUI;
using System;
using System.Collections.Generic;
using Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ScoreSaber.Features.Leaderboards.Services {
    internal class LeaderboardTweeningService {

        private readonly TimeTweeningManager _timeTweeningManager;

        private readonly Dictionary<string, Tween> _activeTweens;

        public LeaderboardTweeningService(TimeTweeningManager timeTweeningManager) {
            _timeTweeningManager = timeTweeningManager;
            _activeTweens = new Dictionary<string, Tween>();
        }

        public void CreateTween<T>(string id, Tween<T> tween, Transform transform) {
            CreateTween(id, (Tween)tween, transform);
        }

        public void CreateFadeTween(string id, float from, float to, float duration, Transform transform) {
            if (transform == null) {
                return;
            }

            CanvasGroup canvasGroup = transform.GetComponent<CanvasGroup>();
            if (canvasGroup == null) {
                return;
            }

            var tween = new FloatTween(from, to, update => {
                SetCanvasGroupAlpha(canvasGroup, update);
            }, duration, EaseType.Linear);
            tween.onCompleted += () => {
                ForgetTween(id, tween);
                SetCanvasGroupAlpha(canvasGroup, to);
            };
            tween.onKilled += () => {
                ForgetTween(id, tween);
                SetCanvasGroupAlpha(canvasGroup, to);
            };
            CreateTween(id, tween, transform);
        }

        public void FadeLayoutGroup(string id, float from, float to, float time, HorizontalOrVerticalLayoutGroup layoutGroup) {
            if (layoutGroup == null) {
                return;
            }

            CanvasRenderer[] canvasRenderers = layoutGroup.transform.GetComponentsInChildren<CanvasRenderer>(true);

            float startAlpha = _activeTweens.ContainsKey(id) && canvasRenderers.Length > 0 ? canvasRenderers[0].GetAlpha() : from;
            float endAlpha = to;

            if (_activeTweens.ContainsKey(id)) {
                KillTween(id);
            }

            foreach (CanvasRenderer canvasRenderer in canvasRenderers) {
                SetRendererAlpha(canvasRenderer, startAlpha);
            }

            var tween = new FloatTween(startAlpha, endAlpha, update => {
                foreach (CanvasRenderer canvasRenderer in canvasRenderers) {
                    SetRendererAlpha(canvasRenderer, update);
                }
            }, time, EaseType.Linear, 0f);

            tween.onCompleted = () => {
                ForgetTween(id, tween);
                if (layoutGroup == null) {
                    return;
                }

                layoutGroup.gameObject.SetActive(to > 0);
                foreach (CanvasRenderer canvasRenderer in canvasRenderers) {
                    SetRendererAlpha(canvasRenderer, endAlpha);
                }
            };
            tween.onKilled = () => {
                ForgetTween(id, tween);
            };

            layoutGroup.gameObject.SetActive(true);
            CreateTween(id, tween, layoutGroup);
        }

        public void CreateImageViewFade(string id, float from, float to, float duration, ImageView imageView) {
            if (imageView == null) {
                return;
            }

            var tween = new FloatTween(from, to, update => {
                SetImageAlpha(imageView, update);
            }, duration, EaseType.Linear);
            tween.onCompleted += () => {
                ForgetTween(id, tween);
                SetImageAlpha(imageView, to);
            };
            tween.onKilled += () => {
                ForgetTween(id, tween);
                SetImageAlpha(imageView, to);
            };
            CreateTween(id, tween, imageView.transform);
        }

        public void CreatePromptTween(string id, float from, float to, float duration, float delay, RectTransform promptRoot, float hiddenY, float visibleY, Action onCompleted = null) {
            if (promptRoot == null) {
                return;
            }

            CanvasGroup canvasGroup = GetOrAddCanvasGroup(promptRoot);
            promptRoot.gameObject.SetActive(true);

            var tween = new FloatTween(from, to, update => {
                SetPromptValue(promptRoot, canvasGroup, hiddenY, visibleY, update);
            }, duration, EaseType.OutCubic, delay);

            tween.onCompleted += () => {
                ForgetTween(id, tween);
                SetPromptValue(promptRoot, canvasGroup, hiddenY, visibleY, to);
                onCompleted?.Invoke();
            };
            tween.onKilled += () => {
                ForgetTween(id, tween);
            };
            CreateTween(id, tween, promptRoot);
        }

        public void CreateFlowTween(string id, Vector3 from, Vector3 to, float duration, Transform transform) {
            if (transform == null) {
                return;
            }

            var tween = new Vector3Tween(from, to, update => {
                transform.localPosition = update;
            }, duration, EaseType.OutCubic);
            tween.onCompleted += () => {
                ForgetTween(id, tween);
                transform.localPosition = to;
            };
            tween.onKilled += () => {
                ForgetTween(id, tween);
                transform.localPosition = to;
            };
            CreateTween(id, tween, transform);
        }

        public void KillTween(string id) {
            if (!_activeTweens.TryGetValue(id, out Tween tween)) {
                return;
            }

            _activeTweens.Remove(id);
            try {
                Action onKilled = tween.onKilled;
                tween.onKilled = null;
                tween.Kill();
                onKilled?.Invoke();
            } catch (Exception ex) {
                Plugin.Log.Warn($"Error killing tween {id}: {ex.Message}");
            }
        }

        public void ClearAllTweens() {
            var keysToRemove = new List<string>(_activeTweens.Keys);
            foreach (string key in keysToRemove) {
                KillTween(key);
            }
        }

        public void ClearTweensByPrefix(string prefix) {
            var keysToRemove = new List<string>();
            foreach (string key in _activeTweens.Keys) {
                if (key.StartsWith(prefix, StringComparison.Ordinal)) {
                    keysToRemove.Add(key);
                }
            }

            foreach (string key in keysToRemove) {
                KillTween(key);
            }
        }

        private void CreateTween(string id, Tween tween, object owner) {
            KillTween(id);
            _activeTweens[id] = tween;
            _timeTweeningManager.AddTween(tween, owner ?? this);
        }

        private void ForgetTween(string id, Tween tween) {
            if (_activeTweens.TryGetValue(id, out Tween activeTween) && activeTween == tween) {
                _activeTweens.Remove(id);
            }
        }

        private static CanvasGroup GetOrAddCanvasGroup(RectTransform transform) {
            CanvasGroup canvasGroup = transform.GetComponent<CanvasGroup>();
            if (canvasGroup == null) {
                canvasGroup = transform.gameObject.AddComponent<CanvasGroup>();
            }

            return canvasGroup;
        }

        private static void SetPromptValue(RectTransform promptRoot, CanvasGroup canvasGroup, float hiddenY, float visibleY, float value) {
            SetCanvasGroupAlpha(canvasGroup, value);
            Vector3 position = promptRoot.localPosition;
            position.y = Mathf.Lerp(hiddenY, visibleY, value);
            promptRoot.localPosition = position;
        }

        private static void SetCanvasGroupAlpha(CanvasGroup canvasGroup, float alpha) {
            if (canvasGroup != null) {
                canvasGroup.alpha = alpha;
            }
        }

        private static void SetImageAlpha(ImageView imageView, float alpha) {
            if (imageView == null) {
                return;
            }

            Color color = imageView.color;
            color.a = alpha;
            imageView.color = color;
        }

        private static void SetRendererAlpha(CanvasRenderer canvasRenderer, float alpha) {
            if (canvasRenderer != null) {
                canvasRenderer.SetAlpha(alpha);
            }
        }
    }
}
