using SevenZip.Compression.LZMA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ScoreSaber.Features.Replays.Format {
    internal class ReplayFileWriter {
        private const int _pointerSize = 38;
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
            bytesWritten += WriteString(metadata.Environment, outputStream);
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
