using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using ScoreSaber.Core;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Live.Ludus.Domain;
using ScoreSaber.Features.Live.Ludus.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ScoreSaber.Features.Live.Ludus.UI {
    [HotReload(RelativePathToLayout = @"./LiveChatFloatingViewController.bsml")]
    internal class LiveChatFloatingViewController : BSMLAutomaticViewController {
        internal const float ChatWidth = 120f;
        internal const float ChatHeight = 140f;

        private const int VisibleMessageCount = 10;
        private const float FooterReserve = 10f;
        private const float FooterWithStatusReserve = 18f;
        private const float TopPadding = 4f;
        private const float MessageRowMinHeight = 8.4f;
        private const float MessageTextWidth = ChatWidth - 11f;
        private const float MessageTextFontSize = 3.4f;
        private const float MessageTextVerticalPadding = 2f;
        private const float TextLineSpacing = 1.5f;
        private const float KeyboardDistance = 0.75f;
        private const float KeyboardVerticalOffset = -0.28f;
        private const float KeyboardWidth = 50f;
        private const float KeyboardHeight = 28f;
        private const float KeyboardContentScale = 0.46f;
        private const float KeyboardDismissWidth = 150f;
        private const float KeyboardDismissHeight = 90f;
        private const float StatusAutoClearSeconds = 3f;
        private const string DefaultStatus = "Spectator chat";

        private static readonly Color ChatBackground = new Color(0f, 0f, 0f, 0.12f);
        private static readonly Color ChatHighlight = new Color(0.980f, 0.800f, 0.082f, 0.08f);
        private static readonly Color ChatAccent = new Color(0.980f, 0.800f, 0.082f, 1f);
        private static readonly Color LinkAccent = new Color(0.18f, 0.75f, 1f, 1f);
        private const string ChatNameColor = "#CDEEFF";
        private const string LogNameColor = "#BBBBBB";
        private const string TimeColor = "#BBBBBB";
        private static readonly Color MessageColor = Color.white;

        private SettingsService _settings;
        private LudusSessionService _ludusSession;
        private LiveChatLinkService _linkService;
        private string _chatDraft = string.Empty;
        private string _status = DefaultStatus;
        private string _viewerStatus = string.Empty;
        private float _appliedTextScale = -1f;
        private int _statusVersion;
        private bool _statusAutoClear;
        private IReadOnlyList<LiveChatEntry> _currentMessages = Array.Empty<LiveChatEntry>();
        private LiveChatFloatingRow[] _visibleRows = Array.Empty<LiveChatFloatingRow>();
        private readonly List<GameObject> _messageObjects = new List<GameObject>();
        private GameObject _chatButtonObject;
        private GameObject _keyboardDismissZonesObject;
        private Coroutine _statusClearCoroutine;
        private TMP_FontAsset _chatFont;
        private TextMeshProUGUI _messageMeasurementText;

        [UIParams]
        private readonly BSMLParserParams _parserParams = null;

        [UIComponent("chat-keyboard")]
        private readonly ModalKeyboard _chatKeyboard = null;

        [UIValue("chat-draft")]
        private string chatDraft {
            get => _chatDraft;
            set => SetValue(ref _chatDraft, value, nameof(chatDraft));
        }

        [UIValue("status")]
        private string status {
            get => _status;
            set {
                SetValue(ref _status, value, nameof(status));
                RenderMessageRows();
            }
        }

        [UIValue("status-font-size")]
        private float statusFontSize => 2.65f * CurrentTextScale;

        [UIValue("empty-font-size")]
        private float emptyFontSize => 3.2f * CurrentTextScale;

        [UIValue("control-font-size")]
        private float controlFontSize => 3.35f * CurrentTextScale;

        [UIValue("small-control-font-size")]
        private float smallControlFontSize => 2.4f * CurrentTextScale;

        [Inject]
        internal void Construct(SettingsService settings, LudusSessionService ludusSession, LiveChatLinkService linkService) {
            _settings = settings;
            _ludusSession = ludusSession;
            _linkService = linkService;
            _linkService.StatusChanged += SetStatus;
            _linkService.ResolvedTextChanged += RebuildMessages;
        }

        protected override void OnDestroy() {
            if (_linkService != null) {
                _linkService.StatusChanged -= SetStatus;
                _linkService.ResolvedTextChanged -= RebuildMessages;
            }
            base.OnDestroy();
        }

        internal void SetMessages(IReadOnlyList<LiveChatEntry> messages) {
            _currentMessages = messages?.ToArray() ?? Array.Empty<LiveChatEntry>();
            RebuildMessages();
        }

        internal void SetViewerCount(int viewerCount) {
            string nextStatus = viewerCount < 0 ? string.Empty : FormatViewerStatus(viewerCount);
            if (_viewerStatus == nextStatus) {
                return;
            }

            _viewerStatus = nextStatus;
            RenderMessageRows();
        }

        internal void RefreshLayoutSettings() {
            float nextTextScale = CurrentTextScale;
            if (Math.Abs(_appliedTextScale - nextTextScale) < 0.001f) {
                return;
            }

            _appliedTextScale = nextTextScale;
            NotifyTextScaleChanged();
            RebuildMessages();
        }

        private void RebuildMessages() {
            _visibleRows = _currentMessages
                .Skip(System.Math.Max(0, _currentMessages.Count - VisibleMessageCount))
                .Select(entry => new LiveChatFloatingRow(entry, _linkService.FirstLink(entry.Text), _linkService.DisplaySenderName(entry), _linkService.DisplayText(entry)))
                .ToArray();

            RenderMessageRows();
        }

        internal void SetStatus(string value) {
            SetStatus(value, ShouldAutoClearStatus(value));
        }

        internal void ResumeStatusAutoClear() {
            StartStatusAutoClearIfReady();
        }

        private void SetStatus(string value, bool autoClear) {
            _statusVersion++;
            status = string.IsNullOrEmpty(value) ? DefaultStatus : value;
            _statusAutoClear = autoClear && ShouldAutoClearStatus(status);

            if (_statusClearCoroutine != null) {
                StopCoroutine(_statusClearCoroutine);
                _statusClearCoroutine = null;
            }

            StartStatusAutoClearIfReady();
        }

        private void StartStatusAutoClearIfReady() {
            if (!_statusAutoClear || _statusClearCoroutine != null || !gameObject.activeInHierarchy) {
                return;
            }

            _statusClearCoroutine = StartCoroutine(ClearStatusAfterDelay(_statusVersion));
        }

        private IEnumerator ClearStatusAfterDelay(int statusVersion) {
            yield return new WaitForSeconds(StatusAutoClearSeconds);
            if (_statusVersion == statusVersion) {
                _statusVersion++;
                _statusAutoClear = false;
                status = DefaultStatus;
            }

            _statusClearCoroutine = null;
        }

        [UIAction("#post-parse")]
        private void Parsed() {
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform != null) {
                rectTransform.pivot = new Vector2(0.5f, 0f);
                rectTransform.sizeDelta = new Vector2(ChatWidth, ChatHeight);
            }

            PositionKeyboard();
            EnsureKeyboardDismissZones();
            EnsureChatButton();
            RenderMessageRows();
        }

        [UIAction("noop")]
        private void Noop() {
        }

        [UIAction("chat-entered")]
        private void ChatEntered(string value) {
            SendChatValue(value);
        }

        [UIAction("send-chat")]
        private void SendChat() {
            SendChatValue(chatDraft);
        }

        private void SendChatValue(string value) {
            string message = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(message)) {
                chatDraft = string.Empty;
                SetStatus("Enter a message first.");
                return;
            }

            if (_ludusSession.SendChatMessage(message)) {
                chatDraft = string.Empty;
                SetStatus(DefaultStatus, false);
            } else {
                SetStatus("Live chat is not connected.");
            }
        }

        [UIAction("decrease-text-size")]
        private void DecreaseTextSize() {
            AdjustTextScale(-0.1f);
        }

        [UIAction("increase-text-size")]
        private void IncreaseTextSize() {
            AdjustTextScale(0.1f);
        }

        [UIAction("decrease-window-size")]
        private void DecreaseWindowSize() {
            AdjustWindowScale(-0.1f);
        }

        [UIAction("increase-window-size")]
        private void IncreaseWindowSize() {
            AdjustWindowScale(0.1f);
        }

        private void OpenLink(LiveChatLinkTarget target) {
            _linkService.Open(target, CancellationToken.None).RunTask();
        }

        private float CurrentTextScale => Clamp(_settings?.Current.liveChatOverlayTextScale ?? 1.25f, 0.9f, 1.8f);

        private void AdjustTextScale(float delta) {
            if (_settings == null) {
                return;
            }

            _settings.Current.liveChatOverlayTextScale = Clamp(_settings.Current.liveChatOverlayTextScale + delta, 0.9f, 1.8f);
            _settings.Save();
            SetStatus("Text " + _settings.Current.liveChatOverlayTextScale.ToString("0.00") + "x");
            _appliedTextScale = -1f;
            RefreshLayoutSettings();
        }

        private void AdjustWindowScale(float delta) {
            if (_settings == null) {
                return;
            }

            _settings.Current.liveChatOverlayScale = Clamp(_settings.Current.liveChatOverlayScale + delta, 0.85f, 1.75f);
            _settings.Save();
            SetStatus("Window " + _settings.Current.liveChatOverlayScale.ToString("0.00") + "x");
        }

        private void NotifyTextScaleChanged() {
            NotifyPropertyChanged(nameof(statusFontSize));
            NotifyPropertyChanged(nameof(emptyFontSize));
            NotifyPropertyChanged(nameof(controlFontSize));
            NotifyPropertyChanged(nameof(smallControlFontSize));
        }

        private void SetValue<T>(ref T field, T value, string propertyName) {
            field = value;
            NotifyPropertyChanged(propertyName);
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

        private static bool ShouldAutoClearStatus(string value) {
            if (string.IsNullOrWhiteSpace(value) || value == DefaultStatus) {
                return false;
            }

            return !StartsWithAny(
                value,
                "Resolving linked map",
                "Checking linked map",
                "Downloading linked map");
        }

        private static bool StartsWithAny(string value, params string[] prefixes) {
            for (int i = 0; i < prefixes.Length; i++) {
                if (value.StartsWith(prefixes[i], StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }

        private static string FormatViewerStatus(int viewerCount) {
            int safeCount = System.Math.Max(0, viewerCount);
            return safeCount == 1 ? "1 viewer" : $"{safeCount} viewers";
        }

        private bool HasStatusLine => !string.IsNullOrWhiteSpace(status) && status != DefaultStatus;

        private void RenderMessageRows() {
            if (transform == null) {
                return;
            }

            EnsureChatButton();
            ClearMessageObjects();
            AddViewerStatusLine();
            AddStatusLine();

            if (_visibleRows.Length == 0) {
                return;
            }

            float y = HasStatusLine ? FooterWithStatusReserve : FooterReserve;
            for (int i = _visibleRows.Length - 1; i >= 0; i--) {
                float rowHeight = RowHeightFor(_visibleRows[i]);
                if (y + rowHeight > ChatHeight - TopPadding) {
                    break;
                }

                AddMessageRow(_visibleRows[i], y, rowHeight);
                y += rowHeight;
            }
        }

        private float RowHeightFor(LiveChatFloatingRow row) {
            float minimumHeight = MessageRowMinHeight * CurrentTextScale;
            float measuredHeight = MeasureMessageTextHeight(row.DisplayText) + MessageTextVerticalPadding;
            if (measuredHeight < minimumHeight) {
                return minimumHeight;
            }

            return measuredHeight;
        }

        private void EnsureChatButton() {
            if (_chatButtonObject != null || transform == null) {
                return;
            }

            _chatButtonObject = new GameObject("Live Chat Open Button", typeof(RectTransform));
            _chatButtonObject.transform.SetParent(transform, false);
            _chatButtonObject.transform.SetAsLastSibling();

            RectTransform rectTransform = _chatButtonObject.transform as RectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.sizeDelta = new Vector2(45f, 7.5f);
            rectTransform.anchoredPosition = new Vector2((ChatWidth * 0.5f) - 24.5f, 1.5f);
            rectTransform.localScale = Vector3.one;

            Image background = _chatButtonObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.22f);
            background.raycastTarget = true;
            ApplyNoGlow(background);

            Button button = _chatButtonObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;
            button.onClick.AddListener(OpenKeyboard);

            AddText(_chatButtonObject.transform, "Send Message", new Color(0.8f, 0.92f, 1f, 0.92f), 2.45f * CurrentTextScale, new Vector2(0f, -0.65f), new Vector2(45f, 7.5f), TextAlignmentOptions.Center);
        }

        private void OpenKeyboard() {
            PositionKeyboard();
            EnsureKeyboardDismissZones();
            _parserParams?.EmitEvent("open-chat-keyboard");
            StartCoroutine(PositionKeyboardNextFrame());
        }

        private IEnumerator PositionKeyboardNextFrame() {
            yield return null;
            PositionKeyboard();
            EnsureKeyboardDismissZones();
        }

        private void PositionKeyboard() {
            if (_chatKeyboard == null) {
                return;
            }

            RectTransform rectTransform = _chatKeyboard.transform as RectTransform;
            if (rectTransform == null) {
                return;
            }

            ApplyKeyboardSizing(rectTransform);

            Camera mainCamera = Camera.main;
            if (mainCamera != null) {
                Transform cameraTransform = mainCamera.transform;
                Vector3 position = cameraTransform.position
                    + (cameraTransform.forward * KeyboardDistance)
                    + (cameraTransform.up * KeyboardVerticalOffset);
                rectTransform.position = position;
                rectTransform.rotation = Quaternion.LookRotation(position - cameraTransform.position, cameraTransform.up);
                rectTransform.localScale = Vector3.one;
                return;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, -58f);
            rectTransform.localScale = Vector3.one;
        }

        private void ApplyKeyboardSizing(RectTransform rectTransform) {
            rectTransform.sizeDelta = new Vector2(KeyboardWidth, KeyboardHeight);
            ApplyKeyboardBackgroundSizing(rectTransform);

            Transform keyboardParent = rectTransform.Find("KeyboardParent");
            if (keyboardParent == null) {
                return;
            }

            RectTransform keyboardParentRect = keyboardParent as RectTransform;
            if (keyboardParentRect != null) {
                keyboardParentRect.anchoredPosition = Vector2.zero;
                keyboardParentRect.sizeDelta = new Vector2(KeyboardWidth, KeyboardHeight);
            }

            keyboardParent.localScale = Vector3.one * KeyboardContentScale;
        }

        private void ApplyKeyboardBackgroundSizing(RectTransform rectTransform) {
            RectTransform background = rectTransform.Find("BG") as RectTransform;
            if (background == null) {
                return;
            }

            background.anchorMin = new Vector2(0.5f, 0.5f);
            background.anchorMax = new Vector2(0.5f, 0.5f);
            background.pivot = new Vector2(0.5f, 0.5f);
            background.anchoredPosition = Vector2.zero;
            background.sizeDelta = new Vector2(KeyboardWidth, KeyboardHeight);
            background.localScale = Vector3.one;
        }

        private void HideKeyboard() {
            _parserParams?.EmitEvent("hide-chat-keyboard");
        }

        private void EnsureKeyboardDismissZones() {
            if (_keyboardDismissZonesObject != null || _chatKeyboard == null) {
                return;
            }

            _keyboardDismissZonesObject = new GameObject("Live Chat Keyboard Outside Dismiss Zones", typeof(RectTransform));
            _keyboardDismissZonesObject.transform.SetParent(_chatKeyboard.transform, false);
            _keyboardDismissZonesObject.transform.SetAsFirstSibling();

            RectTransform root = _keyboardDismissZonesObject.transform as RectTransform;
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = Vector2.zero;
            root.localScale = Vector3.one;

            float sideWidth = (KeyboardDismissWidth - KeyboardWidth) * 0.5f;
            float verticalHeight = (KeyboardDismissHeight - KeyboardHeight) * 0.5f;
            AddKeyboardDismissZone(root, new Vector2(-(KeyboardWidth * 0.5f) - (sideWidth * 0.5f), 0f), new Vector2(sideWidth, KeyboardDismissHeight));
            AddKeyboardDismissZone(root, new Vector2((KeyboardWidth * 0.5f) + (sideWidth * 0.5f), 0f), new Vector2(sideWidth, KeyboardDismissHeight));
            AddKeyboardDismissZone(root, new Vector2(0f, (KeyboardHeight * 0.5f) + (verticalHeight * 0.5f)), new Vector2(KeyboardWidth, verticalHeight));
            AddKeyboardDismissZone(root, new Vector2(0f, -(KeyboardHeight * 0.5f) - (verticalHeight * 0.5f)), new Vector2(KeyboardWidth, verticalHeight));
        }

        private void AddKeyboardDismissZone(Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta) {
            GameObject zoneObject = new GameObject("Dismiss Zone", typeof(RectTransform));
            zoneObject.transform.SetParent(parent, false);

            RectTransform rectTransform = zoneObject.transform as RectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.localScale = Vector3.one;

            Image image = zoneObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            ApplyNoGlow(image);

            Button button = zoneObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            button.onClick.AddListener(HideKeyboard);
        }

        private void ClearMessageObjects() {
            for (int i = _messageObjects.Count - 1; i >= 0; i--) {
                if (_messageObjects[i] != null) {
                    Destroy(_messageObjects[i]);
                }
            }

            _messageObjects.Clear();
        }

        private void AddViewerStatusLine() {
            if (string.IsNullOrWhiteSpace(_viewerStatus)) {
                return;
            }

            GameObject root = new GameObject("Live Chat Viewer Count", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            _messageObjects.Add(root);

            RectTransform rectTransform = root.transform as RectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.sizeDelta = new Vector2(48f, 7.5f);
            rectTransform.anchoredPosition = new Vector2(-35f, 1.5f);
            rectTransform.localScale = Vector3.one;

            Image background = root.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.12f);
            background.raycastTarget = false;
            ApplyNoGlow(background);

            AddAccent(root.transform, ChatAccent, 7.5f);
            AddText(root.transform, _viewerStatus, new Color(0.92f, 0.94f, 0.98f, 0.82f), 2.15f * CurrentTextScale, new Vector2(3f, -0.6f), new Vector2(43f, 7.2f), TextAlignmentOptions.Left);
        }

        private void AddStatusLine() {
            if (!HasStatusLine) {
                return;
            }

            GameObject root = new GameObject("Live Chat Status", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            _messageObjects.Add(root);

            RectTransform rectTransform = root.transform as RectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.sizeDelta = new Vector2(ChatWidth - 9f, 7.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 9.5f);
            rectTransform.localScale = Vector3.one;

            Image background = root.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.22f);
            background.raycastTarget = false;
            ApplyNoGlow(background);

            AddAccent(root.transform, ChatAccent, 7.5f);
            AddText(root.transform, status, new Color(0.92f, 0.94f, 0.98f, 0.95f), 2.3f * CurrentTextScale, new Vector2(3f, -0.6f), new Vector2(ChatWidth - 15f, 7.2f), TextAlignmentOptions.Left);
        }

        private void AddMessageRow(LiveChatFloatingRow row, float y, float height) {
            GameObject root = new GameObject("Live Chat Message", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            _messageObjects.Add(root);

            RectTransform rectTransform = root.transform as RectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.sizeDelta = new Vector2(ChatWidth, height);
            rectTransform.anchoredPosition = new Vector2(0f, y);
            rectTransform.localScale = Vector3.one;

            Image background = root.AddComponent<Image>();
            background.color = row.HasAccent ? ChatHighlight : ChatBackground;
            background.raycastTarget = row.LinkTarget != null;
            ApplyNoGlow(background);

            if (row.LinkTarget != null) {
                Button button = root.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = background;
                LiveChatLinkTarget target = row.LinkTarget;
                button.onClick.AddListener(() => OpenLink(target));
            }

            if (row.HasAccent) {
                AddAccent(root.transform, row.LinkTarget == null ? ChatAccent : LinkAccent, height);
            }

            Vector2 textPosition = new Vector2(6f, -1f);
            Vector2 textSize = new Vector2(MessageTextWidth, height - MessageTextVerticalPadding);
            AddText(root.transform, row.DisplayText, MessageColor, MessageTextFontSize * CurrentTextScale, textPosition, textSize, TextAlignmentOptions.TopLeft, true);
        }

        private void AddAccent(Transform parent, Color color, float height) {
            GameObject accent = new GameObject("Accent", typeof(RectTransform));
            accent.transform.SetParent(parent, false);
            Image image = accent.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            ApplyNoGlow(image);

            RectTransform rect = accent.transform as RectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(1f, height);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private float MeasureMessageTextHeight(string value) {
            TextMeshProUGUI text = MessageMeasurementText;
            if (text == null) {
                return MessageRowMinHeight * CurrentTextScale;
            }

            ConfigureText(text, Color.clear, MessageTextFontSize * CurrentTextScale, TextAlignmentOptions.TopLeft, true);
            text.text = value ?? string.Empty;
            text.rectTransform.sizeDelta = new Vector2(MessageTextWidth, ChatHeight);
            Vector2 preferredValues = text.GetPreferredValues(text.text, MessageTextWidth, float.PositiveInfinity);
            text.text = string.Empty;

            if (float.IsNaN(preferredValues.y) || float.IsInfinity(preferredValues.y) || preferredValues.y <= 0f) {
                return MessageRowMinHeight * CurrentTextScale;
            }

            return preferredValues.y;
        }

        private TextMeshProUGUI MessageMeasurementText {
            get {
                if (_messageMeasurementText != null) {
                    return _messageMeasurementText;
                }

                if (transform == null) {
                    return null;
                }

                GameObject textObject = new GameObject("Live Chat Message Measurement", typeof(RectTransform));
                textObject.transform.SetParent(transform, false);
                _messageMeasurementText = textObject.AddComponent<TextMeshProUGUI>();

                RectTransform rect = _messageMeasurementText.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(MessageTextWidth, ChatHeight);
                rect.localScale = Vector3.one;

                return _messageMeasurementText;
            }
        }

        private TextMeshProUGUI AddText(Transform parent, string value, Color color, float fontSize, Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment, bool wordWrapping = false) {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(text, color, fontSize, alignment, wordWrapping);
            text.text = value ?? string.Empty;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return text;
        }

        private void ConfigureText(TextMeshProUGUI text, Color color, float fontSize, TextAlignmentOptions alignment, bool wordWrapping) {
            text.font = ChatFont;
            text.richText = true;
            text.SetWordWrapping(wordWrapping);
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = alignment;
            text.color = color;
            text.fontSize = fontSize;
            text.lineSpacing = TextLineSpacing;
            text.raycastTarget = false;
        }

        private static void ApplyNoGlow(Image image) {
            if (image == null || Utilities.ImageResources.NoGlowMat == null) {
                return;
            }

            image.material = Utilities.ImageResources.NoGlowMat;
        }

        private TMP_FontAsset ChatFont {
            get {
                if (_chatFont != null) {
                    return _chatFont;
                }

                _chatFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(font => font.name == "Teko-Medium SDF No Glow")
                    ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(font => font.name == "Teko-Medium SDF")
                    ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();
                return _chatFont;
            }
        }

        private sealed class LiveChatFloatingRow {
            internal LiveChatFloatingRow(LiveChatEntry entry, LiveChatLinkTarget linkTarget, string senderName, string text) {
                string title = entry.IsChat ? FirstNonEmpty(senderName, "Unknown") : "Log";
                string detail = Truncate(text, 180);
                Title = title;
                Detail = detail;
                Status = entry.DisplayTime;
                IsChat = entry.IsChat;
                LinkTarget = linkTarget;
                PlainText = $"{Status} {Title}: {Detail}";
                DisplayText = BuildDisplayText(Status, Title, Detail, IsChat);
            }

            internal LiveChatFloatingRow(string title, string detail, string status, bool isChat, LiveChatLinkTarget linkTarget) {
                Title = FirstNonEmpty(title, isChat ? "Unknown" : "Log");
                Detail = detail ?? string.Empty;
                Status = status ?? string.Empty;
                IsChat = isChat;
                LinkTarget = linkTarget;
                PlainText = $"{Status} {Title}: {Detail}";
                DisplayText = BuildDisplayText(Status, Title, Detail, IsChat);
            }

            internal string Title { get; }
            internal string Detail { get; }
            internal string Status { get; }
            internal string DisplayText { get; }
            internal string PlainText { get; }
            internal bool IsChat { get; }
            internal LiveChatLinkTarget LinkTarget { get; }
            internal bool HasAccent => !IsChat || LinkTarget != null;
        }

        private static string BuildDisplayText(string status, string title, string detail, bool isChat) {
            string safeStatus = EscapeRichText(status);
            string safeTitle = EscapeRichText(FirstNonEmpty(title, isChat ? "Unknown" : "Log"));
            string safeDetail = EscapeRichText(detail);
            string nameColor = isChat ? ChatNameColor : LogNameColor;
            string timePrefix = string.IsNullOrEmpty(safeStatus) ? string.Empty : $"<color={TimeColor}>{safeStatus}</color> ";
            return $"{timePrefix}<color={nameColor}><b>{safeTitle}</b></color>: {safeDetail}";
        }

        private static string EscapeRichText(string value) => (value ?? string.Empty).Replace("<", "<\u2060");

        private static string FirstNonEmpty(params string[] values) {
            foreach (string value in values) {
                if (!string.IsNullOrWhiteSpace(value)) {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string Truncate(string value, int maxLength) {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength) {
                return value ?? string.Empty;
            }

            return value.Substring(0, maxLength - 3) + "...";
        }
    }
}
