using System.Runtime.Serialization;

namespace ScoreSaber.Core.Gameplay {
    internal enum ScoreSaberPlayOutcome {
        [EnumMember(Value = "CLEAR")]
        Clear,
        [EnumMember(Value = "FAIL")]
        Fail,
        [EnumMember(Value = "QUIT")]
        Quit,
        [EnumMember(Value = "RESTART")]
        Restart
    }

    internal static class ScoreSaberPlayOutcomes {
        internal static ScoreSaberPlayOutcome FromLevelCompletionResults(LevelCompletionResults results) {
            if (results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed) {
                return ScoreSaberPlayOutcome.Fail;
            }

            if (results.levelEndAction == LevelCompletionResults.LevelEndAction.Restart) {
                return ScoreSaberPlayOutcome.Restart;
            }

            if (results.levelEndAction == LevelCompletionResults.LevelEndAction.Quit) {
                return ScoreSaberPlayOutcome.Quit;
            }

            return ScoreSaberPlayOutcome.Clear;
        }
    }
}
