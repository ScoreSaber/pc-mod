using BeatSaberMarkupLanguage;
using HMUI;
using ScoreSaber.Features.Replays.UI.Components;
using System;
using UnityEngine;
using UnityEngine.UI;
using VRUIControls;
using Zenject;

namespace ScoreSaber.Features.Replays.UI {
    internal class ImberScrubber : IInitializable, ITickable, IDisposable {
        public event Action<float> DidCalculateNewTime;
        private static readonly Color ScoreSaberBlue = new Color(0f, 0.4705882f, 0.7254902f);

        public Transform transform => _parent;

        public bool loopMode {
            get => _loopMode;
            set {
                _loopMode = value;
                _loopNode.gameObject.SetActive(value);
                _bar.AssignNodeToPercent(_loopNode, Mathf.Min(_maxPercent, 1f));
                MainNode_PositionDidChange(_bar.GetNodePercent(_mainNode));

                _mainNode.max = _maxPercent; // uwu owo owo uwu EVENTUALLY REPLACE WITH LEVEL FAILED TIME YEA YEA 
            }
        }

        public bool visibility {
            set => _bar.gameObject.SetActive(value);
        }

        public float mainNodeValue {
            get => _bar.GetNodePercent(_mainNode);
            set => _bar.AssignNodeToPercent(_mainNode, value);
        }

        private AmeBar _bar;
        private AmeNode _mainNode;
        private AmeNode _loopNode;
        private AmeNode _failNode;
        private Transform _parent;
        private bool _loopMode = false;
        private readonly MainCamera _mainCamera;
        private readonly DiContainer _container;
        private readonly float _minNodeDistance = 0.01f;
        private readonly AudioTimeSyncController _audioTimeSyncController;
        private float _levelFailTime = 0f;
        private float _maxPercent = 1f;
        private bool _allowPast;

        public ImberScrubber(MainCamera mainCamera, DiContainer container, AudioTimeSyncController audioTimeSyncController) {

            _container = container;
            _mainCamera = mainCamera;
            _audioTimeSyncController = audioTimeSyncController;
        }

        public void Setup(float levelFailTime, bool allowPast) {

            _levelFailTime = levelFailTime;
            _allowPast = allowPast;
        }

        public void Initialize() {

            _bar = Create(_mainCamera.camera, new Vector2(500f, 100f));
            _bar.transform.localScale = Vector3.one * 0.001f;

            float initialSongTime = _audioTimeSyncController.songTime / _audioTimeSyncController.songEndTime;
            _bar.barFill = initialSongTime;
            _bar.RegisterNode(_mainNode = CreateSlideNode(_bar.transform as RectTransform));
            _bar.RegisterNode(_loopNode = CreateSlideNode(_bar.transform as RectTransform));
            _bar.AssignNodeToPercent(_mainNode, initialSongTime);
            _bar.endTime = _audioTimeSyncController.songEndTime;
            loopMode = _loopMode;

            _mainNode.PositionDidChange += MainNode_PositionDidChange;
            _loopNode.PositionDidChange += LoopNode_PositionDidChange;

            _mainNode.name = "Imber Main Node";
            _loopNode.name = "Imber Loop Node";

            if (_levelFailTime != 0f) {
                _failNode = CreateTextNode(_bar.transform as RectTransform, "FAILED", new Color(0.7f, 0.1f, 0.15f, 1f));
                _failNode.name = "Imber Text Node";
                _failNode.moveable = false;
                if (!_allowPast) {
                    _maxPercent = _levelFailTime / _audioTimeSyncController.songEndTime;
                }
                _bar.AssignNodeToPercent(_failNode, _levelFailTime / _audioTimeSyncController.songEndTime);
                _bar.AssignNodeToPercent(_loopNode, _maxPercent);
                _loopNode.max = _maxPercent;
            }

            _mainNode.max = _bar.GetNodePercent(_loopNode) - _minNodeDistance;
            _loopNode.min = _bar.GetNodePercent(_mainNode) + _minNodeDistance;

            var gameObject = new GameObject("Imber Scrubber Wrapper");
            _bar.gameObject.transform.SetParent(gameObject.transform, false);
            _parent = gameObject.transform;
            gameObject.layer = 5;

            visibility = false;
        }

        private void MainNode_PositionDidChange(float value) {

            _bar.barFill = value;
            DidCalculateNewTime?.Invoke(_audioTimeSyncController.songEndTime * value);
            _bar.currentTime = _audioTimeSyncController.songEndTime * value;
            _loopNode.min = value + _minNodeDistance;
        }

        private void LoopNode_PositionDidChange(float value) {

            _mainNode.max = Mathf.Min(_maxPercent, value) - _minNodeDistance;
        }

        public void Tick() {

            float currentAudioProgress = _audioTimeSyncController.songTime / _audioTimeSyncController.songEndTime;
            if (!_mainNode.isBeingDragged) {
                if (!_loopMode) {
                    mainNodeValue = currentAudioProgress;
                }
                _bar.currentTime = _audioTimeSyncController.songTime;
                _bar.barFill = currentAudioProgress;
            }
            if (_loopMode && currentAudioProgress >= _bar.GetNodePercent(_loopNode)) {
                MainNode_PositionDidChange(mainNodeValue);
            }
        }

        public void Dispose() {

            _mainNode.PositionDidChange -= MainNode_PositionDidChange;
            _loopNode.PositionDidChange -= LoopNode_PositionDidChange;
        }

        #region ALL OBJECT INSTANTIATION EW MANUAL OBJECT SETUP IS CRINGE

        private AmeBar Create(Camera camera, Vector2 size) {
            GameObject ameBar = new GameObject("ImberScrubber: Ame Bar");
            RectTransform rectTransformBar = ameBar.AddComponent<RectTransform>();
            var barSize = new Vector2(size.x, size.y / 10f);
            rectTransformBar.sizeDelta = size;

            Canvas canvas = ameBar.AddComponent<Canvas>();
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 31;

            // And then the canvas's dependencies
            ameBar.AddComponent<CanvasScaler>();
            _container.InstantiateComponent<VRGraphicRaycaster>(ameBar);

            ImageView borderElement = CreateImage(rectTransformBar);
            borderElement.rectTransform.anchorMin = Vector3.zero;
            borderElement.rectTransform.anchorMax = Vector3.one;
            borderElement.rectTransform.sizeDelta = rectTransformBar.sizeDelta * 1.5f;
            borderElement.color = Color.clear;
            borderElement.name = "Box Border";

            ImageView backgroundImage = CreateImage(rectTransformBar);
            backgroundImage.rectTransform.sizeDelta = barSize;
            backgroundImage.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            backgroundImage.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            backgroundImage.color = Color.grey;
            backgroundImage.name = "Background Bar";

            ImageView progressImage = CreateImage(rectTransformBar);
            progressImage.rectTransform.sizeDelta = barSize;
            progressImage.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            progressImage.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            progressImage.color = ScoreSaberBlue;
            progressImage.name = "Progress Bar";

            ImageView clickScrubImage = CreateImage(rectTransformBar);
            clickScrubImage.rectTransform.sizeDelta = new Vector2(barSize.x, barSize.y * 2.25f);
            clickScrubImage.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            clickScrubImage.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            clickScrubImage.color = Color.clear;
            AmeClicker clicker = clickScrubImage.gameObject.AddComponent<AmeClicker>();
            clicker.Setup(ClickedBackground);
            clickScrubImage.name = "Box Click Scrubber";

            AmeBar bar = rectTransformBar.gameObject.AddComponent<AmeBar>();
            bar.Setup(progressImage.rectTransform, backgroundImage.rectTransform);

            return bar;
        }

        private void ClickedBackground(float value) {

            if (!_mainNode.isBeingDragged) {
                DidCalculateNewTime?.Invoke(_audioTimeSyncController.songEndTime * value);
            }
        }

        private ImageView CreateImage(RectTransform transform) {

            GameObject imageGameObject = new GameObject("ImberImage");
            ImageView image = imageGameObject.AddComponent<ImageView>();
            image.material = Utilities.ImageResources.NoGlowMat;
            image.sprite = Utilities.ImageResources.WhitePixel;
            image.rectTransform.SetParent(transform, false);
            return image;
        }

        private AmeNode CreateSlideNode(RectTransform tranform) {

            RectTransform rectTransform = CreateNodeRoot(tranform, "SlideNode");

            ImageView nodeImage = CreateImage(rectTransform);
            nodeImage.rectTransform.sizeDelta = Vector2.one * 25f;
            nodeImage.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nodeImage.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nodeImage.name = "Marker";

            ImageView nodeStem = CreateImage(rectTransform);
            nodeStem.rectTransform.anchoredPosition = new Vector2(0f, 15f);
            nodeStem.rectTransform.sizeDelta = new Vector2(2.5f, 75f);
            nodeStem.rectTransform.anchorMin = Vector2.one / 2f;
            nodeStem.rectTransform.anchorMax = Vector2.one / 2f;
            nodeStem.name = "Stem";

            ImageView nodeHandle = CreateImage(rectTransform);
            nodeHandle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            nodeHandle.rectTransform.anchoredPosition = new Vector2(0f, -25f);
            nodeHandle.rectTransform.sizeDelta = Vector2.one * 30f;
            nodeHandle.rectTransform.anchorMin = Vector2.one / 2f;
            nodeHandle.rectTransform.anchorMax = Vector2.one / 2f;
            nodeHandle.name = "Handle";

            AmeNode node = rectTransform.gameObject.AddComponent<AmeNode>();
            node.Init(nodeHandle.gameObject.AddComponent<AmeHandle>());

            return node;
        }

        private AmeNode CreateTextNode(RectTransform tranform, string initialText, Color color) {

            RectTransform rectTransform = CreateNodeRoot(tranform, "TextNode");

            ImageView nodeImage = CreateImage(rectTransform);
            nodeImage.rectTransform.sizeDelta = Vector2.one * 25f;
            nodeImage.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nodeImage.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nodeImage.name = "Marker";
            nodeImage.color = color;

            GameObject textGameObject = new GameObject("Text");
            textGameObject.transform.SetParent(rectTransform, false);

            CurvedTextMeshPro curvedText = textGameObject.AddComponent<CurvedTextMeshPro>();
            curvedText.font = BeatSaberUI.MainTextFont;
            curvedText.fontSharedMaterial = AmeBar.MainUIFontMaterial;
            curvedText.text = initialText;
            curvedText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            curvedText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            curvedText.alignment = TMPro.TextAlignmentOptions.Top;
            curvedText.color = color;

            AmeNode node = rectTransform.gameObject.AddComponent<AmeNode>();

            return node;
        }

        private static RectTransform CreateNodeRoot(RectTransform parent, string name) {
            GameObject nodeGameObject = new GameObject(name);
            RectTransform rectTransform = nodeGameObject.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchoredPosition = new Vector2(-6f, -50f);
            rectTransform.sizeDelta = Vector2.one * 100f;
            rectTransform.anchorMin = Vector2.one / 2f;
            return rectTransform;
        }
        #endregion
    }
}
