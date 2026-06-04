using HMUI;
using ScoreSaber.Core.Gameplay;
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Replays.Legacy.UI {
    internal class GameReplayUI : MonoBehaviour {

        [Inject] private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData = null;
        [Inject] private readonly ReplayState _replayState = null;

        public void Start() => CreateReplayUI();

        private void CreateReplayUI() {

            string replayText = string.Format("REPLAY MODE - Watching {0} play {1} - {2} ({3})", _replayState.CurrentPlayerName,
                _replayState.CurrentBeatmapLevel.songAuthorName, _replayState.CurrentBeatmapLevel.songName,
              Enum.GetName(typeof(BeatmapDifficulty), _replayState.CurrentBeatmapKey.difficulty).Replace("ExpertPlus", "Expert+"));
            float timeScale = 1f;

            if (!_replayState.IsLegacyReplay) {
                if (_replayState.LoadedReplayFile.noteKeyframes.Count > 0) {
                    timeScale = _replayState.LoadedReplayFile.noteKeyframes[0].TimeSyncTimescale;
                }
            }
            if (timeScale != 1f) {
                replayText += $" [{timeScale:P1}]";
            }
            string friendlyMods = GetFriendlyModifiers(_replayState.CurrentModifiers);
            if (friendlyMods != string.Empty) {
                replayText += string.Format(" [{0}]", friendlyMods);
            }
            GameObject _watermarkCanvas = new GameObject("InGameReplayUI");

            if (_gameplayCoreSceneSetupData.targetEnvironmentInfo.environmentName == "Interscope") {
                _watermarkCanvas.transform.position = new Vector3(0f, 3.5f, 12.0f);
            } else {
                _watermarkCanvas.transform.position = new Vector3(0f, 4f, 12.0f);
            }
            _watermarkCanvas.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);

            Canvas _canvas = _watermarkCanvas.AddComponent<Canvas>();
            _watermarkCanvas.AddComponent<CurvedCanvasSettings>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.enabled = false;
            TMP_Text _text = CreateText(_canvas.transform as RectTransform, replayText, new Vector2(0, 10), new Vector2(100, 20), 15f);
            _text.alignment = TextAlignmentOptions.Center;
            var rectTransform = _text.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            _canvas.enabled = true;
        }

        public TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize) {

            GameObject gameObject = new GameObject("CustomUIText-ScoreSaber");
            gameObject.SetActive(false);
            TextMeshProUGUI textMeshProUGUI = gameObject.AddComponent<TextMeshProUGUI>();
            textMeshProUGUI.font = Instantiate(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First((TMP_FontAsset t) => t.name == "Teko-Medium SDF"));
            textMeshProUGUI.rectTransform.SetParent(parent, false);
            textMeshProUGUI.text = text;
            textMeshProUGUI.fontSize = fontSize;
            textMeshProUGUI.color = Color.white;
            textMeshProUGUI.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            textMeshProUGUI.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textMeshProUGUI.rectTransform.sizeDelta = sizeDelta;
            textMeshProUGUI.rectTransform.anchoredPosition = anchoredPosition;
            gameObject.SetActive(true);
            return textMeshProUGUI;
        }

        public string GetFriendlyModifiers(GameplayModifiers gameplayModifiers) {

            return gameplayModifiers == null ? string.Empty : string.Join(",", ScoreSaberGameplayModifiers.ToCodeList(gameplayModifiers, true));
        }

    }
}
