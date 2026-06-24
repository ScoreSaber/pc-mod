using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ScoreSaber.Features.Replays.Format {
    internal static class ReplayExtensionPayloads {
        internal const string PlaySettingsExtension = "scoresaber.play-settings";
        internal const string PauseEventsExtension = "scoresaber.pause-events";
        internal const string WallEventsExtension = "scoresaber.wall-events";
        internal const string ControllerOffsetsExtension = "scoresaber.controller-offsets";
        internal const string HsvConfigExtension = "scoresaber.hsv-config";

        private delegate int WriteItem<T>(T value, MemoryStream outputStream);

        internal static bool HasFileExtensions(ReplayFile file) {
            return HasPlaySettings(file.metadata)
                || file.pauseKeyframes.Count > 0
                || file.wallKeyframes.Count > 0
                || file.metadata.ControllerOffsets.HasValue
                || (file.hsvConfig != null && file.hsvConfig.Length > 0);
        }

        internal static List<ReplayExtensionEntry> CreateFileExtensions(ReplayFile file) {
            var entries = CreateStartExtensions(file.metadata, file.hsvConfig);
            if (file.pauseKeyframes.Count > 0) {
                entries.Add(CreatePauseEvents(file.pauseKeyframes));
            }
            if (file.wallKeyframes.Count > 0) {
                entries.Add(CreateWallEvents(file.wallKeyframes));
            }

            return entries;
        }

        internal static List<ReplayExtensionEntry> CreateStartExtensions(Metadata metadata, byte[] hsvConfig) {
            var entries = new List<ReplayExtensionEntry>();
            if (HasPlaySettings(metadata)) {
                entries.Add(CreateExtension(PlaySettingsExtension, 1, stream => WritePlaySettings(metadata, stream)));
            }
            if (metadata.ControllerOffsets.HasValue) {
                entries.Add(CreateExtension(ControllerOffsetsExtension, 1, stream => WriteControllerOffsets(metadata.ControllerOffsets.Value, stream)));
            }
            if (hsvConfig != null && hsvConfig.Length > 0) {
                entries.Add(new ReplayExtensionEntry(HsvConfigExtension, 1, hsvConfig));
            }

            return entries;
        }

        internal static ReplayExtensionEntry CreatePauseEvents(IReadOnlyList<PauseEvent> pauseEvents) {
            return CreateExtension(PauseEventsExtension, 1, stream => WriteList(pauseEvents, stream, WritePauseEvent));
        }

        internal static ReplayExtensionEntry CreateWallEvents(IReadOnlyList<WallEvent> wallEvents) {
            return CreateExtension(WallEventsExtension, 1, stream => WriteList(wallEvents, stream, WriteWallEvent));
        }

        private static bool HasPlaySettings(Metadata metadata) {
            return metadata.HasPlaySettingsExtension
                || !string.IsNullOrEmpty(metadata.Environment)
                || metadata.SongSpeed > 0f
                || metadata.JumpDistance > 0f
                || metadata.LeftSaberColor.HasValue
                || metadata.RightSaberColor.HasValue
                || metadata.ObstacleColor.HasValue
                || metadata.EnvironmentColor0.HasValue
                || metadata.EnvironmentColor1.HasValue
                || metadata.EnvironmentColorW.HasValue
                || metadata.EnvironmentColor0Boost.HasValue
                || metadata.EnvironmentColor1Boost.HasValue
                || metadata.EnvironmentColorWBoost.HasValue;
        }

        private static ReplayExtensionEntry CreateExtension(string id, int version, Action<MemoryStream> writePayload) {
            using (var stream = new MemoryStream()) {
                writePayload(stream);
                return new ReplayExtensionEntry(id, version, stream.ToArray());
            }
        }

        private static int WritePlaySettings(Metadata metadata, MemoryStream outputStream) {
            int bytesWritten = 0;
            bytesWritten += WriteFloat(metadata.SongSpeed, outputStream);
            bytesWritten += WriteFloat(metadata.JumpDistance, outputStream);
            bytesWritten += WriteColor(metadata.LeftSaberColor, outputStream);
            bytesWritten += WriteColor(metadata.RightSaberColor, outputStream);
            bytesWritten += WriteColor(metadata.ObstacleColor, outputStream);
            bytesWritten += WriteColor(metadata.EnvironmentColor0, outputStream);
            bytesWritten += WriteColor(metadata.EnvironmentColor1, outputStream);
            bytesWritten += WriteColor(metadata.EnvironmentColorW, outputStream);
            bytesWritten += WriteColor(metadata.EnvironmentColor0Boost, outputStream);
            bytesWritten += WriteColor(metadata.EnvironmentColor1Boost, outputStream);
            bytesWritten += WriteColor(metadata.EnvironmentColorWBoost, outputStream);
            bytesWritten += WriteBool(metadata.SupportsEnvironmentColorBoost, outputStream);
            bytesWritten += WriteString(metadata.Environment ?? string.Empty, outputStream);
            bytesWritten += WriteInt(metadata.EnvironmentEffectsFilterDefaultPreset, outputStream);
            bytesWritten += WriteInt(metadata.EnvironmentEffectsFilterExpertPlusPreset, outputStream);
            bytesWritten += WriteInt(metadata.EnvironmentEffectsFilterPreset, outputStream);
            bytesWritten += WriteBool(metadata.NoTextsAndHuds, outputStream);
            bytesWritten += WriteFloat(metadata.SaberTrailIntensity, outputStream);
            bytesWritten += WriteBool(metadata.HideNoteSpawnEffect, outputStream);
            bytesWritten += WriteBool(metadata.ArcsHapticFeedback, outputStream);
            bytesWritten += WriteInt(metadata.ArcVisibility, outputStream);
            return bytesWritten;
        }

        private static int WritePauseEvent(PauseEvent pauseEvent, MemoryStream outputStream) {
            int bytesWritten = 0;
            bytesWritten += WriteFloat(pauseEvent.Time, outputStream);
            bytesWritten += WriteLong(pauseEvent.Duration, outputStream);
            bytesWritten += WriteLong(pauseEvent.UnixStartTime, outputStream);
            bytesWritten += WriteLong(pauseEvent.UnixEndTime, outputStream);
            return bytesWritten;
        }

        private static int WriteWallEvent(WallEvent wallEvent, MemoryStream outputStream) {
            int bytesWritten = 0;
            bytesWritten += WriteFloat(wallEvent.Time, outputStream);
            bytesWritten += WriteFloat(wallEvent.ExitTime, outputStream);
            bytesWritten += WriteFloat(wallEvent.Energy, outputStream);
            bytesWritten += WriteFloat(wallEvent.ObstacleTime, outputStream);
            bytesWritten += WriteFloat(wallEvent.ObstacleDuration, outputStream);
            bytesWritten += WriteInt(wallEvent.LineIndex, outputStream);
            bytesWritten += WriteInt(wallEvent.LineLayer, outputStream);
            bytesWritten += WriteInt(wallEvent.Width, outputStream);
            bytesWritten += WriteInt(wallEvent.Height, outputStream);
            return bytesWritten;
        }

        private static int WriteControllerOffsets(ReplayControllerOffsets offsets, MemoryStream outputStream) {
            int bytesWritten = 0;
            bytesWritten += WriteControllerOffset(offsets.Shared, outputStream);
            bytesWritten += WriteControllerOffset(offsets.Left, outputStream);
            bytesWritten += WriteControllerOffset(offsets.Right, outputStream);
            return bytesWritten;
        }

        private static int WriteControllerOffset(ReplayControllerOffset? offset, MemoryStream outputStream) {
            int bytesWritten = 0;
            bytesWritten += WriteBool(offset.HasValue, outputStream);
            if (offset.HasValue) {
                ReplayControllerOffset value = offset.Value;
                bytesWritten += WriteVRPosition(value.Position, outputStream);
                bytesWritten += WriteVRPosition(value.Rotation, outputStream);
            }
            return bytesWritten;
        }

        private static int WriteList<T>(IReadOnlyList<T> values, MemoryStream outputStream, WriteItem<T> writeItem) {
            int bytesWritten = 0;
            bytesWritten += WriteInt(values.Count, outputStream);
            foreach (T value in values) {
                bytesWritten += writeItem(value, outputStream);
            }
            return bytesWritten;
        }

        private static int WriteString(string value, MemoryStream outputStream) {
            int bytesWritten = 0;
            byte[] stringBytes = Encoding.UTF8.GetBytes(value);
            bytesWritten += WriteInt(stringBytes.Length, outputStream);
            outputStream.Write(stringBytes, 0, stringBytes.Length);
            bytesWritten += stringBytes.Length;
            return bytesWritten;
        }

        private static int WriteInt(int value, MemoryStream outputStream) {
            outputStream.WriteByte((byte)value);
            outputStream.WriteByte((byte)(value >> 8));
            outputStream.WriteByte((byte)(value >> 16));
            outputStream.WriteByte((byte)(value >> 24));
            return 4;
        }

        private static int WriteFloat(float value, MemoryStream outputStream) {
            return WriteInt(new FloatIntUnion { Float = value }.Int, outputStream);
        }

        private static int WriteBool(bool value, MemoryStream outputStream) {
            outputStream.WriteByte(value ? (byte)1 : (byte)0);
            return 1;
        }

        private static int WriteLong(long value, MemoryStream outputStream) {
            for (int i = 0; i < 8; i++) {
                outputStream.WriteByte((byte)(value >> (8 * i)));
            }
            return 8;
        }

        private static int WriteColor(UnityEngine.Color? color, MemoryStream outputStream) {
            int bytesWritten = 0;
            bytesWritten += WriteBool(color.HasValue, outputStream);
            if (color.HasValue) {
                UnityEngine.Color value = color.Value;
                bytesWritten += WriteFloat(value.r, outputStream);
                bytesWritten += WriteFloat(value.g, outputStream);
                bytesWritten += WriteFloat(value.b, outputStream);
                bytesWritten += WriteFloat(value.a, outputStream);
            }
            return bytesWritten;
        }

        private static int WriteVRPosition(VRPosition position, MemoryStream outputStream) {
            int bytesWritten = 0;
            bytesWritten += WriteFloat(position.X, outputStream);
            bytesWritten += WriteFloat(position.Y, outputStream);
            bytesWritten += WriteFloat(position.Z, outputStream);
            return bytesWritten;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FloatIntUnion {
            [FieldOffset(0)]
            internal float Float;

            [FieldOffset(0)]
            internal int Int;
        }
    }

    internal class ReplayExtensionEntry {
        internal ReplayExtensionEntry(string id, int version, byte[] payload) {
            Id = id;
            Version = version;
            Payload = payload;
        }

        internal string Id { get; }
        internal int Version { get; }
        internal byte[] Payload { get; }
    }
}
