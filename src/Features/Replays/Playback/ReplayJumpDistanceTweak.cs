#if BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Replays.Format;
using SiraUtil.Affinity;

namespace ScoreSaber.Features.Replays.Playback {
    internal class ReplayJumpDistanceTweak : IAffinity {
        private readonly ReplayFile _file;
        private readonly SettingsService _settings;

        public ReplayJumpDistanceTweak(ReplayFile file, SettingsService settings) {

            _file = file;
            _settings = settings;
        }

        [AffinityPrefix, AffinityPatch(typeof(VariableMovementDataProvider), nameof(VariableMovementDataProvider.Init))]
        private void MovementDataInitPrefix(ref float noteJumpMovementSpeed, ref BeatmapObjectSpawnMovementData.NoteJumpValueType noteJumpValueType, ref float noteJumpValue) {

            if (!_settings.Current.useRecordedPlayerSettings || !_file.metadata.HasPlaySettingsExtension || _file.metadata.JumpDistance <= 0f || noteJumpMovementSpeed <= 0f) {
                return;
            }

            noteJumpValueType = BeatmapObjectSpawnMovementData.NoteJumpValueType.JumpDuration;
            noteJumpValue = _file.metadata.JumpDistance / noteJumpMovementSpeed / 2f;
        }
    }
}
#endif
