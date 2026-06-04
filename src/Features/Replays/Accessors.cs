using HMUI;
using IPA.Utilities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace ScoreSaber.Features.Replays {
#pragma warning disable IDE1006 // Naming Styles
    internal static class Accessors {
        internal static readonly FieldAccessor<ComboController, int>.Accessor Combo = FieldAccessor<ComboController, int>.GetAccessor("_combo");
        internal static readonly FieldAccessor<ComboController, int>.Accessor MaxCombo = FieldAccessor<ComboController, int>.GetAccessor("_maxCombo");
        internal static readonly FieldAccessor<ComboUIController, int>.Accessor TriggerID = FieldAccessor<ComboUIController, int>.GetAccessor("_comboLostId");
        internal static readonly FieldAccessor<ComboUIController, bool>.Accessor ComboWasLost = FieldAccessor<ComboUIController, bool>.GetAccessor("_fullComboLost");
        internal static readonly FieldAccessor<ComboUIController, Animator>.Accessor ComboAnimator = FieldAccessor<ComboUIController, Animator>.GetAccessor("_animator");

        internal static readonly FieldAccessor<GameEnergyUIPanel, Image>.Accessor EnergyBar = FieldAccessor<GameEnergyUIPanel, Image>.GetAccessor("_energyBar");
        internal static readonly PropertyAccessor<GameEnergyCounter, float>.Setter ActiveEnergy = PropertyAccessor<GameEnergyCounter, float>.GetSetter("energy");
        internal static readonly FieldAccessor<GameEnergyCounter, bool>.Accessor DidReachZero = FieldAccessor<GameEnergyCounter, bool>.GetAccessor("_didReach0Energy");
        internal static readonly PropertyAccessor<GameEnergyCounter, bool>.Setter NoFailPropertyUpdater = PropertyAccessor<GameEnergyCounter, bool>.GetSetter("noFail");
        internal static readonly FieldAccessor<GameEnergyCounter, float>.Accessor NextEnergyChange = FieldAccessor<GameEnergyCounter, float>.GetAccessor("_nextFrameEnergyChange");
        internal static readonly FieldAccessor<GameEnergyUIPanel, PlayableDirector>.Accessor Director = FieldAccessor<GameEnergyUIPanel, PlayableDirector>.GetAccessor("_playableDirector");

        internal static readonly FieldAccessor<ScoreMultiplierCounter, int>.Accessor Multiplier = FieldAccessor<ScoreMultiplierCounter, int>.GetAccessor("_multiplier");
        internal static readonly FieldAccessor<ScoreMultiplierCounter, int>.Accessor Progress = FieldAccessor<ScoreMultiplierCounter, int>.GetAccessor("_multiplierIncreaseProgress");
        internal static readonly FieldAccessor<ScoreMultiplierCounter, int>.Accessor MaxProgress = FieldAccessor<ScoreMultiplierCounter, int>.GetAccessor("_multiplierIncreaseMaxProgress");
        internal static readonly FieldAccessor<ScoreController, ScoreMultiplierCounter>.Accessor MultiplierCounter = FieldAccessor<ScoreController, ScoreMultiplierCounter>.GetAccessor("_scoreMultiplierCounter");

        internal static readonly FieldAccessor<SaberSwingRatingCounter, float>.Accessor AfterCutRating = FieldAccessor<SaberSwingRatingCounter, float>.GetAccessor("_afterCutRating");
        internal static readonly FieldAccessor<SaberSwingRatingCounter, float>.Accessor BeforeCutRating = FieldAccessor<SaberSwingRatingCounter, float>.GetAccessor("_beforeCutRating");
        internal static readonly FieldAccessor<BasicBeatmapObjectManager, MemoryPoolContainer<BombNoteController>>.Accessor BombNotePool = FieldAccessor<BasicBeatmapObjectManager, MemoryPoolContainer<BombNoteController>>.GetAccessor("_bombNotePoolContainer");
        internal static readonly FieldAccessor<BasicBeatmapObjectManager, MemoryPoolContainer<GameNoteController>>.Accessor BurstSliderHeadNotePool = FieldAccessor<BasicBeatmapObjectManager, MemoryPoolContainer<GameNoteController>>.GetAccessor("_burstSliderHeadGameNotePoolContainer");
        internal static readonly FieldAccessor<BasicBeatmapObjectManager, MemoryPoolContainer<BurstSliderGameNoteController>>.Accessor BurstSliderNotePool = FieldAccessor<BasicBeatmapObjectManager, MemoryPoolContainer<BurstSliderGameNoteController>>.GetAccessor("_burstSliderGameNotePoolContainer");
        internal static readonly FieldAccessor<BasicBeatmapObjectManager, MemoryPoolContainer<GameNoteController>>.Accessor GameNotePool = FieldAccessor<BasicBeatmapObjectManager, MemoryPoolContainer<GameNoteController>>.GetAccessor("_basicGameNotePoolContainer");
        internal static readonly FieldAccessor<SaberSwingRatingCounter, LazyCopyHashSet<ISaberSwingRatingCounterDidChangeReceiver>>.Accessor ChangeReceivers = FieldAccessor<SaberSwingRatingCounter, LazyCopyHashSet<ISaberSwingRatingCounterDidChangeReceiver>>.GetAccessor("_didChangeReceivers");

        internal static readonly FieldAccessor<ScoreController, int>.Accessor MultipliedScore = FieldAccessor<ScoreController, int>.GetAccessor("_multipliedScore");
        internal static readonly FieldAccessor<ScoreController, int>.Accessor ImmediateMultipliedPossible = FieldAccessor<ScoreController, int>.GetAccessor("_immediateMaxPossibleMultipliedScore");
        internal static readonly FieldAccessor<ScoreController, int>.Accessor ModifiedScore = FieldAccessor<ScoreController, int>.GetAccessor("_modifiedScore");
        internal static readonly FieldAccessor<ScoreController, int>.Accessor ImmediateModifiedPossible = FieldAccessor<ScoreController, int>.GetAccessor("_immediateMaxPossibleModifiedScore");
        internal static readonly FieldAccessor<ScoreController, float>.Accessor GameplayMultiplier = FieldAccessor<ScoreController, float>.GetAccessor("_prevMultiplierFromModifiers");
        internal static readonly FieldAccessor<ScoreController, GameplayModifiersModelSO>.Accessor ModifiersModelSO = FieldAccessor<ScoreController, GameplayModifiersModelSO>.GetAccessor("_gameplayModifiersModel");
        internal static readonly FieldAccessor<ScoreController, List<GameplayModifierParamsSO>>.Accessor ModifierPanelsSO = FieldAccessor<ScoreController, List<GameplayModifierParamsSO>>.GetAccessor("_gameplayModifierParams");

        internal static PropertyAccessor<RelativeScoreAndImmediateRankCounter, RankModel.Rank>.Setter ImmediateRank = PropertyAccessor<RelativeScoreAndImmediateRankCounter, RankModel.Rank>.GetSetter("immediateRank");
        internal static PropertyAccessor<RelativeScoreAndImmediateRankCounter, float>.Setter RelativeScore = PropertyAccessor<RelativeScoreAndImmediateRankCounter, float>.GetSetter("relativeScore");

        internal static readonly FieldAccessor<MenuTransitionsHelper, StandardLevelScenesTransitionSetupDataSO>.Accessor StandardLevelScenesTransitionSetupData = FieldAccessor<MenuTransitionsHelper, StandardLevelScenesTransitionSetupDataSO>.GetAccessor("_standardLevelScenesTransitionSetupData");

        internal static readonly FieldAccessor<BeatmapCallbacksController, Dictionary<float, CallbacksInTime>>.Accessor CallbacksInTime = FieldAccessor<BeatmapCallbacksController, Dictionary<float, CallbacksInTime>>.GetAccessor("_callbacksInTimes");
        internal static readonly FieldAccessor<BeatmapCallbacksController, float>.Accessor CallbackStartFilterTime = FieldAccessor<BeatmapCallbacksController, float>.GetAccessor("_startFilterTime");
        internal static readonly FieldAccessor<BeatmapCallbacksController.InitData, float>.Accessor InitialStartFilterTime = FieldAccessor<BeatmapCallbacksController.InitData, float>.GetAccessor("startFilterTime");
        internal static readonly FieldAccessor<NoteCutSoundEffectManager, MemoryPoolContainer<NoteCutSoundEffect>>.Accessor NoteCutPool = FieldAccessor<NoteCutSoundEffectManager, MemoryPoolContainer<NoteCutSoundEffect>>.GetAccessor("_noteCutSoundEffectPoolContainer");

        internal static readonly FieldAccessor<NoteCutSoundEffectManager, AudioManagerSO>.Accessor AudioManager = FieldAccessor<NoteCutSoundEffectManager, AudioManagerSO>.GetAccessor("_audioManager");
        internal static readonly FieldAccessor<AudioTimeSyncController, float>.Accessor AudioStartOffset = FieldAccessor<AudioTimeSyncController, float>.GetAccessor("_audioStartTimeOffsetSinceStart");
        internal static readonly FieldAccessor<AudioTimeSyncController, int>.Accessor AudioLoopIndex = FieldAccessor<AudioTimeSyncController, int>.GetAccessor("_playbackLoopIndex");
        internal static readonly FieldAccessor<AudioTimeSyncController, float>.Accessor AudioTimeScale = FieldAccessor<AudioTimeSyncController, float>.GetAccessor("_timeScale");
        internal static readonly FieldAccessor<AudioTimeSyncController, float>.Accessor AudioSongTime = FieldAccessor<AudioTimeSyncController, float>.GetAccessor("_songTime");
        public static readonly FieldAccessor<AudioTimeSyncController, AudioSource>.Accessor AudioSource = FieldAccessor<AudioTimeSyncController, AudioSource>.GetAccessor("_audioSource");

        internal static readonly FieldAccessor<ColorNoteVisuals, Color>.Accessor NoteColor = FieldAccessor<ColorNoteVisuals, Color>.GetAccessor("_noteColor");
        internal static readonly PropertyAccessor<ColorNoteVisuals, bool>.Setter SetArrowVisibility = PropertyAccessor<ColorNoteVisuals, bool>.GetSetter("showArrow");
        internal static readonly PropertyAccessor<ColorNoteVisuals, bool>.Setter SetCircleVisibility = PropertyAccessor<ColorNoteVisuals, bool>.GetSetter("showCircle");
        internal static readonly FieldAccessor<ColorNoteVisuals, MaterialPropertyBlockController[]>.Accessor NoteMaterialBlocks = FieldAccessor<ColorNoteVisuals, MaterialPropertyBlockController[]>.GetAccessor("_materialPropertyBlockControllers");

        internal static readonly FieldAccessor<ResultsViewController, LevelCompletionResults>.Accessor resultsViewControllerLevelCompletionResults = FieldAccessor<ResultsViewController, LevelCompletionResults>.GetAccessor("_levelCompletionResults");

        internal static readonly FieldAccessor<ModalView, bool>.Accessor animateParentCanvas = FieldAccessor<ModalView, bool>.GetAccessor("_animateParentCanvas");
        internal static readonly FieldAccessor<PlayerTransforms, Transform>.Accessor HeadTransform = FieldAccessor<PlayerTransforms, Transform>.GetAccessor("_headTransform");

        internal static readonly FieldAccessor<CutScoreBuffer, SaberSwingRatingCounter>.Accessor RatingCounter = FieldAccessor<CutScoreBuffer, SaberSwingRatingCounter>.GetAccessor("_saberSwingRatingCounter");
        internal static readonly PropertyAccessor<ScoringElement, bool>.Setter ScoringElementFinisher = PropertyAccessor<ScoringElement, bool>.GetSetter("isFinished");
    }
}
