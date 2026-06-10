using System;

namespace ScoreSaber.Features.Replays.Format {
    internal enum ScoringType_pre1_40 {
        Ignore = -1,
        NoScore,
        Normal,
        SliderHead,
        SliderTail,
        BurstSliderHead,
        BurstSliderElement
    }
    internal enum ScoringType_1_40 {
        Ignore = -1,
        NoScore,
        Normal,
        ArcHead,
        ArcTail,
        ChainHead,
        ChainLink,
        ArcHeadArcTail,
        ChainHeadArcTail,
        ChainLinkArcHead,
        ChainHeadArcHead,
        ChainHeadArcHeadArcTail
    }

    // replays keep values from the game that recorded them, so compare by note parts
    internal static class ReplayScoringTypes {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_38_0
        private const bool GameUses1_40Values = false;
#else
        private const bool GameUses1_40Values = true;
#endif

        [Flags]
        private enum NoteParts {
            None = 0,
            ArcHead = 1,
            ArcTail = 2,
            ChainHead = 4,
            ChainLink = 8
        }

        internal static bool Matches(int storedScoringType, bool storedUses1_40Values, NoteData.ScoringType gameScoringType) {
            if (storedUses1_40Values == GameUses1_40Values) {
                return storedScoringType == (int)gameScoringType;
            }

            int legacy = storedUses1_40Values ? (int)gameScoringType : storedScoringType;
            int modern = storedUses1_40Values ? storedScoringType : (int)gameScoringType;
            NoteParts legacyParts = LegacyParts((ScoringType_pre1_40)legacy);
            // these values line up in both eras
            return legacyParts == NoteParts.None ? legacy == modern : (legacyParts & ModernParts((ScoringType_1_40)modern)) != 0;
        }

        private static NoteParts LegacyParts(ScoringType_pre1_40 scoringType) {
            switch (scoringType) {
                case ScoringType_pre1_40.SliderHead: return NoteParts.ArcHead;
                case ScoringType_pre1_40.SliderTail: return NoteParts.ArcTail;
                case ScoringType_pre1_40.BurstSliderHead: return NoteParts.ChainHead;
                case ScoringType_pre1_40.BurstSliderElement: return NoteParts.ChainLink;
                default: return NoteParts.None;
            }
        }

        private static NoteParts ModernParts(ScoringType_1_40 scoringType) {
            switch (scoringType) {
                case ScoringType_1_40.ArcHead: return NoteParts.ArcHead;
                case ScoringType_1_40.ArcTail: return NoteParts.ArcTail;
                case ScoringType_1_40.ChainHead: return NoteParts.ChainHead;
                case ScoringType_1_40.ChainLink: return NoteParts.ChainLink;
                case ScoringType_1_40.ArcHeadArcTail: return NoteParts.ArcHead | NoteParts.ArcTail;
                case ScoringType_1_40.ChainHeadArcTail: return NoteParts.ChainHead | NoteParts.ArcTail;
                case ScoringType_1_40.ChainLinkArcHead: return NoteParts.ChainLink | NoteParts.ArcHead;
                case ScoringType_1_40.ChainHeadArcHead: return NoteParts.ChainHead | NoteParts.ArcHead;
                case ScoringType_1_40.ChainHeadArcHeadArcTail: return NoteParts.ChainHead | NoteParts.ArcHead | NoteParts.ArcTail;
                default: return NoteParts.None;
            }
        }
    }
}
