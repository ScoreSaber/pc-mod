using Legato.Gameplay.Movement;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Replays.Format;
using System;
using Zenject;

namespace ScoreSaber.Features.Replays.Playback {
    internal sealed class ReplayMovementDataEventHandler : IInitializable, IDisposable {
        private readonly ReplayFile _file;
        private readonly SettingsService _settings;

        public ReplayMovementDataEventHandler(ReplayFile file, SettingsService settings) {
            _file = file;
            _settings = settings;
        }

        public void Initialize() => MovementDataEvents.Initializing += HandleMovementDataInitializing;

        public void Dispose() => MovementDataEvents.Initializing -= HandleMovementDataInitializing;

        private void HandleMovementDataInitializing(
            ref float noteJumpMovementSpeed,
            ref BeatmapObjectSpawnMovementData.NoteJumpValueType noteJumpValueType,
            ref float noteJumpValue) {
            if (!_settings.Current.useRecordedPlayerSettings || !_file.metadata.HasPlaySettingsExtension || _file.metadata.JumpDistance <= 0f || noteJumpMovementSpeed <= 0f) {
                return;
            }

            noteJumpValueType = BeatmapObjectSpawnMovementData.NoteJumpValueType.JumpDuration;
            noteJumpValue = _file.metadata.JumpDistance / noteJumpMovementSpeed / 2f;
        }
    }
}
