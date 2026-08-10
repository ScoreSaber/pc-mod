namespace ScoreSaber.Features.Replays.Format {
    internal static partial class ReplayScoringTypes {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
        private const ScoringTypeEra GameEra = ScoringTypeEra.Pre1_40;
#elif BEAT_SABER_1_40_0
        private const ScoringTypeEra GameEra = ScoringTypeEra.From1_40_0;
#else
        private const ScoringTypeEra GameEra = ScoringTypeEra.From1_40_9;
#endif
    }
}
