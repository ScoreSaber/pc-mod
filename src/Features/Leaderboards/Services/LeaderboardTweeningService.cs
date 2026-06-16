using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ScoreSaber.Features.Leaderboards.Services {
    internal class LeaderboardTweeningService {

        private readonly TimeTweeningManager _timeTweeningManager;

        private readonly Dictionary<string, Tween> activeScoreSaberRotations;

        public LeaderboardTweeningService(TimeTweeningManager timeTweeningManager) {
            _timeTweeningManager = timeTweeningManager;
            activeScoreSaberRotations = new Dictionary<string, Tween>();
        }

        public void CreateTween<T>(string id, Tween<T> tween, Transform transform) {
            if(activeScoreSaberRotations.ContainsKey(id)) {
                KillTween(id);
            }
            activeScoreSaberRotations[id] = tween;
            _timeTweeningManager.AddTween(tween, this);
        }

        public void CreateFadeTween(string id, float from, float to, float duration, Transform transform) {
            var tween = new FloatTween(from, to, update => {
                var canvasGroup = transform.GetComponent<CanvasGroup>();
                if (canvasGroup != null) {
                    canvasGroup.alpha = update;
                }
            }, duration, EaseType.Linear);
            tween.onCompleted += () => {
                if (activeScoreSaberRotations.ContainsKey(id)) {
                    activeScoreSaberRotations.Remove(id);
                }
                var canvasGroup = transform.GetComponent<CanvasGroup>();
                if (canvasGroup != null) {
                    canvasGroup.alpha = to;
                }
            };
            tween.onKilled += () => {
                if (activeScoreSaberRotations.ContainsKey(id)) {
                    activeScoreSaberRotations.Remove(id);
                }
                var canvasGroup = transform.GetComponent<CanvasGroup>();
                if (canvasGroup != null) {
                    canvasGroup.alpha = to;
                }
            };
            CreateTween(id, tween, transform);
        }

        public void FadeLayoutGroup(string id, float from, float to, float time, HorizontalOrVerticalLayoutGroup layoutGroup) {
            List<CanvasRenderer> canvasRenderers = new List<CanvasRenderer>();
            canvasRenderers = layoutGroup.transform.GetComponentsInChildren<CanvasRenderer>().ToList();

            float startAlpha = activeScoreSaberRotations.ContainsKey(id) && canvasRenderers.Count > 0 ? canvasRenderers[0].GetAlpha() : from;
            float endAlpha = to;

            if (activeScoreSaberRotations.ContainsKey(id)) {
                KillTween(id);
            }

            foreach (CanvasRenderer canvasRenderer in canvasRenderers) {
                canvasRenderer.SetAlpha(startAlpha);
            }

            Tween tween = new Tweening.FloatTween(startAlpha, endAlpha, (float u) => {
                foreach (CanvasRenderer canvasRenderer in canvasRenderers) {
                    canvasRenderer.SetAlpha(u);
                }
            }, time, EaseType.Linear, 0f);

            tween.onCompleted = () => {
                if (activeScoreSaberRotations.ContainsKey(id)) {
                    activeScoreSaberRotations.Remove(id);
                }
                if (layoutGroup == null) return;
                layoutGroup.gameObject.SetActive(to > 0);
                foreach (CanvasRenderer canvasRenderer in canvasRenderers) {
                    canvasRenderer.SetAlpha(endAlpha);
                }
            };
            tween.onKilled = () => {
                if (activeScoreSaberRotations.ContainsKey(id)) {
                    activeScoreSaberRotations.Remove(id);
                }
                if (layoutGroup == null) return;
            };

            layoutGroup.gameObject.SetActive(true);
            activeScoreSaberRotations[id] = tween;
            _timeTweeningManager.AddTween(tween, layoutGroup);
        }
    

        public void CreateImageViewFade(string id, float from, float to, float duration, ImageView imageView) {
            var tween = new FloatTween(from, to, update => {
                imageView.color = new Color(imageView.color.r, imageView.color.g, imageView.color.b, update);
            }, duration, EaseType.Linear);
            tween.onCompleted += () => {
                if (activeScoreSaberRotations.ContainsKey(id)) {
                    activeScoreSaberRotations.Remove(id);
                }
                imageView.color = new Color(imageView.color.r, imageView.color.g, imageView.color.b, to);
            };
            tween.onKilled += () => {
                if (activeScoreSaberRotations.ContainsKey(id)) {
                    activeScoreSaberRotations.Remove(id);
                }
                imageView.color = new Color(imageView.color.r, imageView.color.g, imageView.color.b, to);
            };
            CreateTween(id, tween, imageView.transform);
        }

        public void CreatePromptTween(string id, float from, float to, float duration, float delay, RectTransform promptRoot, float hiddenY, float visibleY, Action onCompleted = null) {
            CanvasGroup canvasGroup = promptRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) {
                canvasGroup = promptRoot.gameObject.AddComponent<CanvasGroup>();
            }

            var tween = new FloatTween(from, to, update => {
                canvasGroup.alpha = update;
                promptRoot.gameObject.SetActive(true);
                promptRoot.localPosition = new Vector3(promptRoot.localPosition.x, Mathf.Lerp(hiddenY, visibleY, update), promptRoot.localPosition.z);
            }, duration, EaseType.OutCubic, delay);

            tween.onCompleted += () => {
                if (activeScoreSaberRotations.ContainsKey(id)) {
                    activeScoreSaberRotations.Remove(id);
                }
                canvasGroup.alpha = to;
                promptRoot.localPosition = new Vector3(promptRoot.localPosition.x, Mathf.Lerp(hiddenY, visibleY, to), promptRoot.localPosition.z);
                onCompleted?.Invoke();
            };
            tween.onKilled += () => {
                if (activeScoreSaberRotations.ContainsKey(id)) {
                    activeScoreSaberRotations.Remove(id);
                }
            };
            CreateTween(id, tween, promptRoot);
        }

        public void CreateFlowTween(string id, Vector3 from, Vector3 to, float duration, Transform transform) {
            var tween = new Vector3Tween(from, to, update => {
                transform.localPosition = update;
            }, duration, EaseType.OutCubic);
            tween.onCompleted += () => {
                if (activeScoreSaberRotations.ContainsKey(id)) {
                    activeScoreSaberRotations.Remove(id);
                }
                transform.localPosition = to;
            };
            tween.onKilled += () => {
                if (activeScoreSaberRotations.ContainsKey(id)) {
                    activeScoreSaberRotations.Remove(id);
                }
                transform.localPosition = to;
            };
            CreateTween(id, tween, transform);
        }

        public void KillTween(string id) {
            if (activeScoreSaberRotations.TryGetValue(id, out var tween)) {
                try {
                    tween.Kill();
                    tween.onKilled?.Invoke();
                } catch (Exception ex) {
                    Plugin.Log.Warn($"Error killing tween {id}: {ex.Message}");
                }
                activeScoreSaberRotations.Remove(id);
            }
        }

        public void ClearAllTweens() {
            foreach (var tween in activeScoreSaberRotations.Values) {
                tween.Kill();
            }
            activeScoreSaberRotations.Clear();
        }

        public void ClearTweensByPrefix(string prefix) {
            var keysToRemove = activeScoreSaberRotations.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in keysToRemove) {
                KillTween(key);
            }
        }
    }
}
