using SevenZip.Compression.LZMA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ScoreSaber.Features.Replays.Format {
    internal class ReplayFileWriter {
        private const int _pointerSize = 38;
        private const int ExtensionMagic = 0x31585353; // SSX1
        private const int ExtensionTableVersion = 1;
        private static readonly byte[] FileHeader = Encoding.UTF8.GetBytes("ScoreSaber Replay 👌🤠\r\n");
        private delegate int WriteItem<T>(T value, MemoryStream outputStream);

        internal byte[] Write(ReplayFile file) {

            try {
                byte[] compressed = null;
                using (var outputStream = new MemoryStream()) {

                    int pointerLocation = (int)outputStream.Length;
                    for (int i = 0; i < _pointerSize; i += 4) {
                        WriteInt(0, outputStream);
                    }

                    int metadataPointer = (int)outputStream.Length;
                    WriteMetadata(file.metadata, outputStream);
                    int poseKeyframePointer = (int)outputStream.Length;

                    WriteList(file.poseKeyframes, outputStream, WriteVRPoseGroup);
                    int heightEventsPointer = (int)outputStream.Length;
                    WriteList(file.heightKeyframes, outputStream, WriteHeightEvent);
                    int noteEventsPointer = (int)outputStream.Length;
                    WriteList(file.noteKeyframes, outputStream, WriteNoteEvent);
                    int scoreEventsPointer = (int)outputStream.Length;
                    WriteList(file.scoreKeyframes, outputStream, WriteScoreEvent);
                    int comboEventsPointer = (int)outputStream.Length;
                    WriteList(file.comboKeyframes, outputStream, WriteComboEvent);
                    int multiplierEventsPointer = (int)outputStream.Length;
                    WriteList(file.multiplierKeyframes, outputStream, WriteMultiplierEvent);
                    int energyEventsPointer = (int)outputStream.Length;
                    WriteList(file.energyKeyframes, outputStream, WriteEnergyEvent);
                    int extensionsPointer = 0;
                    if (HasExtensions(file)) {
                        extensionsPointer = (int)outputStream.Length;
                        WriteExtensions(file, outputStream);
                    }

                    // Write pointers
                    outputStream.Position = pointerLocation;
                    WriteInt(metadataPointer, outputStream);
                    WriteInt(poseKeyframePointer, outputStream);
                    WriteInt(heightEventsPointer, outputStream);
                    WriteInt(noteEventsPointer, outputStream);
                    WriteInt(scoreEventsPointer, outputStream);
                    WriteInt(comboEventsPointer, outputStream);
                    WriteInt(multiplierEventsPointer, outputStream);
                    WriteInt(energyEventsPointer, outputStream);
                    WriteInt(extensionsPointer, outputStream);
                    byte[] uncompressed = outputStream.ToArray();
                    compressed = SevenZipHelper.Compress(uncompressed);
                }
                byte[] result = new byte[FileHeader.Length + compressed.Length];
                Buffer.BlockCopy(FileHeader, 0, result, 0, FileHeader.Length);
                Buffer.BlockCopy(compressed, 0, result, FileHeader.Length, compressed.Length);
                return result;
            } catch (Exception ex) {
                //File.WriteAllText("replay.json", Newtonsoft.Json.JsonConvert.SerializeObject(file));
                Plugin.Log.Debug($"Failed to write replay: {ex.ToString()}");
                return null;
            }
        }

        private int WriteMetadata(Metadata metadata, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteString(metadata.Version.ToString(), outputStream);
            bytesWritten += WriteString(metadata.LevelID, outputStream);
            bytesWritten += WriteInt(metadata.Difficulty, outputStream);
            bytesWritten += WriteString(metadata.Characteristic, outputStream);
            bytesWritten += WriteString(metadata.Environment ?? string.Empty, outputStream);
            bytesWritten += WriteStringArray(metadata.Modifiers, outputStream);
            bytesWritten += WriteFloat(metadata.NoteSpawnOffset, outputStream);
            bytesWritten += WriteBool(metadata.LeftHanded, outputStream);
            bytesWritten += WriteFloat(metadata.InitialHeight, outputStream);
            bytesWritten += WriteFloat(metadata.RoomRotation, outputStream);
            bytesWritten += WriteVRPosition(metadata.RoomCenter, outputStream);
            bytesWritten += WriteFloat(metadata.FailTime, outputStream);
            bytesWritten += WriteString(metadata.GameVersion.ToString(), outputStream);
            bytesWritten += WriteString(metadata.PluginVersion.ToString(), outputStream);
            bytesWritten += WriteString(metadata.Platform, outputStream);
            return bytesWritten;
        }

        private int WriteExtensions(ReplayFile file, MemoryStream outputStream) {

            List<ReplayExtensionEntry> entries = ReplayExtensionPayloads.CreateFileExtensions(file);

            int bytesWritten = 0;
            bytesWritten += WriteInt(ExtensionMagic, outputStream);
            bytesWritten += WriteInt(ExtensionTableVersion, outputStream);
            bytesWritten += WriteInt(entries.Count, outputStream);
            foreach (ReplayExtensionEntry entry in entries) {
                bytesWritten += WriteString(entry.Id, outputStream);
                bytesWritten += WriteInt(entry.Version, outputStream);
                bytesWritten += WriteByteArray(entry.Payload, outputStream);
            }
            return bytesWritten;
        }

        private static bool HasExtensions(ReplayFile file) {

            return ReplayExtensionPayloads.HasFileExtensions(file);
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

        private int WritePlaySettings(Metadata metadata, MemoryStream outputStream) {

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

        private int WritePauseEvent(PauseEvent pauseEvent, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteFloat(pauseEvent.Time, outputStream);
            bytesWritten += WriteLong(pauseEvent.Duration, outputStream);
            bytesWritten += WriteLong(pauseEvent.UnixStartTime, outputStream);
            bytesWritten += WriteLong(pauseEvent.UnixEndTime, outputStream);
            return bytesWritten;
        }

        private int WriteWallEvent(WallEvent wallEvent, MemoryStream outputStream) {

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

        private int WriteControllerOffsets(ReplayControllerOffsets offsets, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteControllerOffset(offsets.Shared, outputStream);
            bytesWritten += WriteControllerOffset(offsets.Left, outputStream);
            bytesWritten += WriteControllerOffset(offsets.Right, outputStream);
            return bytesWritten;
        }

        private int WriteControllerOffset(ReplayControllerOffset? offset, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteBool(offset.HasValue, outputStream);
            if (offset.HasValue) {
                ReplayControllerOffset value = offset.Value;
                bytesWritten += WriteVRPosition(value.Position, outputStream);
                bytesWritten += WriteVRPosition(value.Rotation, outputStream);
            }
            return bytesWritten;
        }

        private int WriteVRPoseGroup(VRPoseGroup vrPoseGroup, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteVRPose(vrPoseGroup.Head, outputStream);
            bytesWritten += WriteVRPose(vrPoseGroup.Left, outputStream);
            bytesWritten += WriteVRPose(vrPoseGroup.Right, outputStream);
            bytesWritten += WriteInt(vrPoseGroup.FPS, outputStream);
            bytesWritten += WriteFloat(vrPoseGroup.Time, outputStream);
            return bytesWritten;
        }

        private int WriteVRPose(VRPose vrPose, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteVRPosition(vrPose.Position, outputStream);
            bytesWritten += WriteVRRotation(vrPose.Rotation, outputStream);
            return bytesWritten;
        }

        private int WriteHeightEvent(HeightEvent heightEvent, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteFloat(heightEvent.Height, outputStream);
            bytesWritten += WriteFloat(heightEvent.Time, outputStream);
            return bytesWritten;
        }

        private int WriteNoteEvent(NoteEvent noteEvent, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteNoteID(noteEvent.NoteID, outputStream);
            bytesWritten += WriteInt((int)noteEvent.EventType, outputStream);
            bytesWritten += WriteVRPosition(noteEvent.CutPoint, outputStream);
            bytesWritten += WriteVRPosition(noteEvent.CutNormal, outputStream);
            bytesWritten += WriteVRPosition(noteEvent.SaberDirection, outputStream);
            bytesWritten += WriteInt(noteEvent.SaberType, outputStream);
            bytesWritten += WriteBool(noteEvent.DirectionOK, outputStream);
            bytesWritten += WriteFloat(noteEvent.SaberSpeed, outputStream);
            bytesWritten += WriteFloat(noteEvent.CutAngle, outputStream);
            bytesWritten += WriteFloat(noteEvent.CutDistanceToCenter, outputStream);
            bytesWritten += WriteFloat(noteEvent.CutDirectionDeviation, outputStream);
            bytesWritten += WriteFloat(noteEvent.BeforeCutRating, outputStream);
            bytesWritten += WriteFloat(noteEvent.AfterCutRating, outputStream);
            bytesWritten += WriteFloat(noteEvent.Time, outputStream);
            bytesWritten += WriteFloat(noteEvent.UnityTimescale, outputStream);
            bytesWritten += WriteFloat(noteEvent.TimeSyncTimescale, outputStream);

            bytesWritten += WriteFloat(noteEvent.TimeDeviation.Value, outputStream);
            bytesWritten += WriteVRRotation(noteEvent.WorldRotation.Value, outputStream);
            bytesWritten += WriteVRRotation(noteEvent.InverseWorldRotation.Value, outputStream);
            bytesWritten += WriteVRRotation(noteEvent.NoteRotation.Value, outputStream);
            bytesWritten += WriteVRPosition(noteEvent.NotePosition.Value, outputStream);
            return bytesWritten;
        }

        private int WriteNoteID(NoteID noteID, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteFloat(noteID.Time, outputStream);
            bytesWritten += WriteInt(noteID.LineLayer, outputStream);
            bytesWritten += WriteInt(noteID.LineIndex, outputStream);
            bytesWritten += WriteInt(noteID.ColorType, outputStream);
            bytesWritten += WriteInt(noteID.CutDirection, outputStream);
            bytesWritten += WriteInt(noteID.GameplayType.Value, outputStream);
            bytesWritten += WriteInt(noteID.ScoringType.Value, outputStream);
            bytesWritten += WriteFloat(noteID.CutDirectionAngleOffset.Value, outputStream);
            return bytesWritten;
        }

        private int WriteScoreEvent(ScoreEvent scoreEvent, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteInt(scoreEvent.Score, outputStream);
            bytesWritten += WriteFloat(scoreEvent.Time, outputStream);
            bytesWritten += WriteInt(scoreEvent.ImmediateMaxPossibleScore.Value, outputStream);
            return bytesWritten;
        }

        private int WriteComboEvent(ComboEvent scoreEvent, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteInt(scoreEvent.Combo, outputStream);
            bytesWritten += WriteFloat(scoreEvent.Time, outputStream);
            return bytesWritten;
        }

        private int WriteMultiplierEvent(MultiplierEvent multiplierEvent, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteInt(multiplierEvent.Multiplier, outputStream);
            bytesWritten += WriteFloat(multiplierEvent.NextMultiplierProgress, outputStream);
            bytesWritten += WriteFloat(multiplierEvent.Time, outputStream);
            return bytesWritten;
        }

        private int WriteEnergyEvent(EnergyEvent energyEvent, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteFloat(energyEvent.Energy, outputStream);
            bytesWritten += WriteFloat(energyEvent.Time, outputStream);
            return bytesWritten;
        }

        // Lists
        private int WriteStringArray(string[] values, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteInt(values.Length, outputStream);
            foreach (string value in values) {
                bytesWritten += WriteString(value, outputStream);
            }
            return bytesWritten;
        }

        private int WriteList<T>(List<T> values, MemoryStream outputStream, WriteItem<T> writeItem) {

            int bytesWritten = 0;
            bytesWritten += WriteInt(values.Count, outputStream);
            foreach (T value in values) {
                bytesWritten += writeItem(value, outputStream);
            }
            return bytesWritten;
        }

        // Primitives
        private int WriteString(string value, MemoryStream outputStream) {

            int bytesWritten = 0;
            byte[] stringBytes = Encoding.UTF8.GetBytes(value);
            bytesWritten += WriteInt(stringBytes.Length, outputStream);

            outputStream.Write(stringBytes, 0, stringBytes.Length);
            bytesWritten += stringBytes.Length;

            return bytesWritten;
        }

        private int WriteInt(int value, MemoryStream outputStream) {

            outputStream.WriteByte((byte)value);
            outputStream.WriteByte((byte)(value >> 8));
            outputStream.WriteByte((byte)(value >> 16));
            outputStream.WriteByte((byte)(value >> 24));
            return 4;
        }

        private int WriteFloat(float value, MemoryStream outputStream) {

            return WriteInt(new FloatIntUnion { Float = value }.Int, outputStream);
        }

        private int WriteBool(bool value, MemoryStream outputStream) {

            outputStream.WriteByte(value ? (byte)1 : (byte)0);
            return 1;
        }

        private int WriteLong(long value, MemoryStream outputStream) {

            for (int i = 0; i < 8; i++) {
                outputStream.WriteByte((byte)(value >> (8 * i)));
            }
            return 8;
        }

        private int WriteByteArray(byte[] value, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteInt(value.Length, outputStream);
            outputStream.Write(value, 0, value.Length);
            bytesWritten += value.Length;
            return bytesWritten;
        }

        private int WriteColor(UnityEngine.Color? color, MemoryStream outputStream) {

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

        private int WriteVRPosition(VRPosition position, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteFloat(position.X, outputStream);
            bytesWritten += WriteFloat(position.Y, outputStream);
            bytesWritten += WriteFloat(position.Z, outputStream);
            return bytesWritten;
        }

        private int WriteVRRotation(VRRotation rotation, MemoryStream outputStream) {

            int bytesWritten = 0;
            bytesWritten += WriteFloat(rotation.X, outputStream);
            bytesWritten += WriteFloat(rotation.Y, outputStream);
            bytesWritten += WriteFloat(rotation.Z, outputStream);
            bytesWritten += WriteFloat(rotation.W, outputStream);
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
}
