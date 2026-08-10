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
        ChainHeadArcHead, // 1.40.9+
        ChainHeadArcHeadArcTail
    }

    internal enum ScoringTypeEra {
        Pre1_40,
        From1_40_0,
        From1_40_9
    }

    // replays keep values from the game that recorded them, so compare by note parts
    internal static partial class ReplayScoringTypes {

        [Flags]
        private enum NoteParts {
            None = 0,
            ArcHead = 1,
            ArcTail = 2,
            ChainHead = 4,
            ChainLink = 8
        }

        internal static ScoringTypeEra EraOf(Hive.Versioning.Version gameVersion) {
            if (gameVersion == null || gameVersion < RelevantGameVersions.Version_1_40) {
                return ScoringTypeEra.Pre1_40;
            }
            return gameVersion < RelevantGameVersions.Version_1_40_9 ? ScoringTypeEra.From1_40_0 : ScoringTypeEra.From1_40_9;
        }

        internal static bool Matches(int storedScoringType, ScoringTypeEra storedEra, NoteData.ScoringType gameScoringType) {
            if (storedEra == GameEra) {
                return storedScoringType == (int)gameScoringType;
            }

            NoteParts storedParts = Parts(storedScoringType, storedEra);
            NoteParts gameParts = Parts((int)gameScoringType, GameEra);
            // partless values line up in every era
            return storedParts == NoteParts.None || gameParts == NoteParts.None
                ? storedScoringType == (int)gameScoringType
                : (storedParts & gameParts) != 0;
        }

        private static NoteParts Parts(int scoringType, ScoringTypeEra era) {
            return era == ScoringTypeEra.Pre1_40 ? LegacyParts((ScoringType_pre1_40)scoringType) : ModernParts((ScoringType_1_40)scoringType);
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
