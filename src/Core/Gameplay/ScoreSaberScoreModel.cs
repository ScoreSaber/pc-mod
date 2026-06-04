namespace ScoreSaber.Core.Gameplay {
    internal static class ScoreSaberScoreModel {
        internal static int OldMaxRawScoreForNumberOfNotes(int noteCount) {
            int num = 0;
            int num2 = 1;
            while (num2 < 8) {
                if (noteCount >= num2 * 2) {
                    num += num2 * num2 * 2 + num2;
                    noteCount -= num2 * 2;
                    num2 *= 2;
                    continue;
                }
                num += num2 * noteCount;
                noteCount = 0;
                break;
            }
            num += noteCount * num2;
            return num * 115;
        }
    }
}
