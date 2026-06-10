namespace ScoreSaber.Core.Compat {
    // this property arrived in 1.38; the old one is the same data
    internal static class SaberCompat {
        internal static SaberMovementData GetMovementDataForLogic(this Saber saber) =>
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1
            saber.movementData;
#else
            saber.movementDataForLogic;
#endif
    }
}
