using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.FloatingScreen;
using HMUI;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Live.Compete.Services;
using ScoreSaber.Features.Live.Ludus.Domain;
using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Live.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace ScoreSaber.Features.Live.Ludus.UI {
    internal sealed class LiveChatOverlayController : IInitializable, ITickable, IDisposable {
        private readonly SettingsService _settings;
        private readonly LudusSessionService _ludusSession;
        private readonly CompeteGameplayState _competeGameplayState;
        private readonly LiveChatFloatingViewController _viewController;

        private FloatingScreen _screen;
        private Vector3 _baseScale = Vector3.one;
        private float _appliedOverlayScale = -1f;
        private IReadOnlyList<LiveChatEntry> _pendingMessages = Array.Empty<LiveChatEntry>();
        private string _pendingStatus;
        private int _pendingViewerCount = int.MinValue;
        private bool _hasChatMessages;
        private bool _visible;
        private bool _messagesDirty;
        private bool _statusDirty;
        private bool _viewerCountDirty;

        internal LiveChatOverlayController(
            SettingsService settings,
            LudusSessionService ludusSession,
            CompeteGameplayState competeGameplayState,
            LiveChatFloatingViewController viewController) {

            _settings = settings;
            _ludusSession = ludusSession;
            _competeGameplayState = competeGameplayState;
            _viewController = viewController;
        }

        public void Initialize() {
            _screen = FloatingScreen.CreateFloatingScreen(
                new Vector2(LiveChatFloatingViewController.ChatWidth, LiveChatFloatingViewController.ChatHeight),
                true,
                new Vector3(0f, 3.75f, 2.5f),
                Quaternion.Euler(325f, 0f, 0f));
            _screen.name = "ScoreSaber Live Chat Overlay";
            UnityEngine.Object.DontDestroyOnLoad(_screen.gameObject);
            _screen.GetComponent<Canvas>().sortingOrder = 33;
            _baseScale = _screen.transform.localScale;
            StyleFloatingScreen();
            ApplyOverlayScale(true);
            _screen.gameObject.SetActive(false);
            _ludusSession.ChatMessagesChanged += ChatMessagesChanged;
            _ludusSession.StatusChanged += StatusChanged;
            _ludusSession.ViewerListUpdated += ViewerListUpdated;
            _ludusSession.PlayerFollowRequested += PlayerFollowRequested;
            StoreMessages(_ludusSession.CurrentChatMessages);
            StoreViewerCount(_ludusSession.CurrentViewerCount);
            Plugin.Log.Info($"Live chat overlay initialized. enabled={_settings.Current.liveChatOverlayEnabled} connected={_ludusSession.IsConnectedToLudus}");
            ApplyVisibility(false);
        }

        public void Tick() {
            ApplyOverlayScale(false);
            ApplyVisibility(true);
            if (!_visible) {
                return;
            }

            _viewController.RefreshLayoutSettings();
            StoreViewerCount(_ludusSession.CurrentViewerCount);
            FlushViewStateIfVisible();
        }

        public void Dispose() {
            _ludusSession.ChatMessagesChanged -= ChatMessagesChanged;
            _ludusSession.StatusChanged -= StatusChanged;
            _ludusSession.ViewerListUpdated -= ViewerListUpdated;
            _ludusSession.PlayerFollowRequested -= PlayerFollowRequested;
            if (_screen != null) {
                UnityEngine.Object.Destroy(_screen.gameObject);
                _screen = null;
            }
        }

        private void ChatMessagesChanged(IReadOnlyList<LiveChatEntry> messages) {
            StoreMessages(messages);
            ApplyVisibility(true);
        }

        private void StatusChanged(string status) {
            if (_pendingStatus == status) {
                return;
            }

            _pendingStatus = status;
            _statusDirty = true;
            FlushViewStateIfVisible();
        }

        private void ViewerListUpdated(IReadOnlyList<LiveRoomViewerState> viewers) {
            StoreViewerCount(viewers?.Count ?? 0);
        }

        private void PlayerFollowRequested(int viewerCount) {
            StoreViewerCount(viewerCount);
        }

        private void StoreMessages(IReadOnlyList<LiveChatEntry> messages) {
            _pendingMessages = messages?.ToArray() ?? Array.Empty<LiveChatEntry>();
            _hasChatMessages = HasChatMessages(_pendingMessages);
            _messagesDirty = true;
            FlushViewStateIfVisible();
        }

        private void StoreViewerCount(int viewerCount) {
            int nextViewerCount = _ludusSession.IsInPublicPresence ? viewerCount : -1;
            if (_pendingViewerCount == nextViewerCount) {
                return;
            }

            _pendingViewerCount = nextViewerCount;
            _viewerCountDirty = true;
            FlushViewStateIfVisible();
        }

        private void FlushViewStateIfVisible() {
            if (!_visible) {
                return;
            }

            if (_messagesDirty) {
                _viewController.SetMessages(_pendingMessages);
                _messagesDirty = false;
            }
            if (_statusDirty) {
                _viewController.SetStatus(_pendingStatus);
                _statusDirty = false;
            }
            if (_viewerCountDirty) {
                _viewController.SetViewerCount(_pendingViewerCount);
                _viewerCountDirty = false;
            }
        }

        private void ApplyOverlayScale(bool force) {
            if (_screen == null) {
                return;
            }

            float nextScale = Clamp(_settings.Current.liveChatOverlayScale, 0.85f, 1.75f);
            if (!force && Math.Abs(_appliedOverlayScale - nextScale) < 0.001f) {
                return;
            }

            _appliedOverlayScale = nextScale;
            _screen.transform.localScale = _baseScale * nextScale;
        }

        private void ApplyVisibility(bool animated) {
            bool shouldShow = ShouldShowOverlay();
            if (_screen == null) {
                return;
            }

            if (_visible == shouldShow) {
                _screen.gameObject.SetActive(shouldShow);
                return;
            }

            _visible = shouldShow;
            if (_visible) {
                _screen.gameObject.SetActive(true);
                FlushViewStateIfVisible();
                _screen.SetRootViewController(_viewController, animated ? ViewController.AnimationType.In : ViewController.AnimationType.None);
                _viewController.ResumeStatusAutoClear();
                Plugin.Log.Info($"Live chat overlay shown. connected={_ludusSession.IsConnectedToLudus}");
                return;
            }

            _screen.SetRootViewController(null, animated ? ViewController.AnimationType.Out : ViewController.AnimationType.None);
            _screen.gameObject.SetActive(false);
            Plugin.Log.Info("Live chat overlay hidden.");
        }

        private bool ShouldShowOverlay() {
            if (!_settings.Current.liveChatOverlayEnabled) {
                return false;
            }

            if (_competeGameplayState.IsLiveGameplayActive) {
                return false;
            }

            bool gameplaySceneActive = IsGameplaySceneActive();
            if (_ludusSession.IsInTournamentRoom) {
                return !gameplaySceneActive;
            }

            if (!_hasChatMessages) {
                return false;
            }

            return !gameplaySceneActive || _settings.Current.liveChatOverlayGameplayEnabled;
        }

        private static bool HasChatMessages(IReadOnlyList<LiveChatEntry> messages) {
            return messages != null && messages.Any(message => message?.IsChat == true);
        }

        private static bool IsGameplaySceneActive() {
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name == "GameCore") {
                    return true;
                }
            }

            return SceneManager.GetActiveScene().name == "GameCore";
        }

        private static float Clamp(float value, float min, float max) {
            if (value < min) {
                return min;
            }

            if (value > max) {
                return max;
            }

            return value;
        }

        private void StyleFloatingScreen() {
            if (_screen == null) {
                return;
            }

            Image background = _screen.GetComponent<Image>();
            if (background != null) {
                background.material = CreateNoGlowMaterial(Color.white);
                background.color = new Color(0f, 0f, 0f, 0.5f);
                background.raycastTarget = false;
            }

            GameObject handleObject = _screen.GetHandle();
            Transform handle = handleObject?.transform;
            if (handle != null) {
                handle.localScale = new Vector2(8f, LiveChatFloatingViewController.ChatHeight);
                handle.localPosition = new Vector3((-LiveChatFloatingViewController.ChatWidth * 0.5f) - 5f, 0f, 0f);
                handle.localRotation = Quaternion.identity;
            }

            Renderer renderer = handleObject?.GetComponent<Renderer>();
            if (renderer != null) {
                renderer.material = CreateNoGlowMaterial(Color.clear);
            }
        }

        private static Material CreateNoGlowMaterial(Color color) {
            Material source = Utilities.ImageResources.NoGlowMat;
            Material material = source == null ? new Material(Shader.Find("UI/Default")) : UnityEngine.Object.Instantiate(source);
            if (material != null) {
                material.color = color;
            }

            return material;
        }
    }
}
