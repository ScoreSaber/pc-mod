using IPA.Utilities;
using SevenZip.Compression.LZMA;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScoreSaber.Features.Replays.Format {
    internal class Pointers {
        internal int metadata;
        internal int poseKeyframes;
        internal int heightKeyframes;
        internal int noteKeyframes;
        internal int scoreKeyframes;
        internal int comboKeyframes;
        internal int multiplierKeyframes;
        internal int energyKeyframes;
        internal int fpsKeyframes;
    }

    internal class ReplayVersionException : Exception {
        public ReplayVersionException() { }
        public ReplayVersionException(string message) : base(message) { }
        public ReplayVersionException(string message, Exception innerException) : base(message, innerException) { }
    }

    internal class ReplayFileReader {
        private static readonly byte[] FileHeader = Encoding.UTF8.GetBytes("ScoreSaber Replay 👌🤠\r\n");
        private static readonly Version ReplayVersion2 = new Version("2.0.0");
        private static readonly Version ReplayVersion3Max = new Version("3.1.0");

        private byte[] _input;
        private delegate T ReadItem<T>(ref int offset);

        internal ReplayFile Read(byte[] input) {

            if (!HasFileHeader(input)) {
                throw new ReplayVersionException("Unknown replay magic bytes");
            }

            byte[] compressedInput = new byte[input.Length - FileHeader.Length];
            Buffer.BlockCopy(input, FileHeader.Length, compressedInput, 0, compressedInput.Length);
            _input = compressedInput;
            _input = SevenZipHelper.Decompress(_input);
            Pointers pointers = ReadPointers();

            var metadata = ReadMetadata(ref pointers.metadata);

            if (metadata.Version > ReplayVersion3Max) {
                throw new ReplayVersionException("Unknown replay version");
            }

            bool usesV2Events = metadata.Version == ReplayVersion2;
            return new ReplayFile() {
                metadata = metadata,
                poseKeyframes = ReadList(ref pointers.poseKeyframes, ReadVRPoseGroup),
                heightKeyframes = ReadList(ref pointers.heightKeyframes, ReadHeightChange),
                noteKeyframes = usesV2Events ? ReadList(ref pointers.noteKeyframes, ReadNoteEvent) : ReadList(ref pointers.noteKeyframes, ReadNoteEvent_v3),
                scoreKeyframes = usesV2Events ? ReadList(ref pointers.scoreKeyframes, ReadScoreEvent) : ReadList(ref pointers.scoreKeyframes, ReadScoreEvent_v3),
                comboKeyframes = ReadList(ref pointers.comboKeyframes, ReadComboEvent),
                multiplierKeyframes = ReadList(ref pointers.multiplierKeyframes, ReadMultiplierEvent),
                energyKeyframes = ReadList(ref pointers.energyKeyframes, ReadEnergyEvent)
            };
        }

        private static bool HasFileHeader(byte[] input) {
            if (input == null || input.Length < FileHeader.Length) {
                return false;
            }

            for (int i = 0; i < FileHeader.Length; i++) {
                if (input[i] != FileHeader[i]) {
                    return false;
                }
            }

            return true;
        }

        private Pointers ReadPointers() {

            int offset = 0;
            return new Pointers() {
                metadata = ReadInt(ref offset),
                poseKeyframes = ReadInt(ref offset),
                heightKeyframes = ReadInt(ref offset),
                noteKeyframes = ReadInt(ref offset),
                scoreKeyframes = ReadInt(ref offset),
                comboKeyframes = ReadInt(ref offset),
                multiplierKeyframes = ReadInt(ref offset),
                energyKeyframes = ReadInt(ref offset),
                fpsKeyframes = ReadInt(ref offset)
            };
        }

        private Metadata ReadMetadata(ref int offset) {
            Version version = new Version(ReadString(ref offset));

            if (version < ReplayVersion3Max) {
                return new Metadata() {
                    Version = version,
                    LevelID = ReadString(ref offset),
                    Difficulty = ReadInt(ref offset),
                    Characteristic = ReadString(ref offset),
                    Environment = ReadString(ref offset),
                    Modifiers = ReadStringArray(ref offset),
                    NoteSpawnOffset = ReadFloat(ref offset),
                    LeftHanded = ReadBool(ref offset),
                    InitialHeight = ReadFloat(ref offset),
                    RoomRotation = ReadFloat(ref offset),
                    RoomCenter = ReadVRPosition(ref offset),
                    FailTime = ReadFloat(ref offset)
                };
            } else {
                return new Metadata() {
                    Version = version,
                    LevelID = ReadString(ref offset),
                    Difficulty = ReadInt(ref offset),
                    Characteristic = ReadString(ref offset),
                    Environment = ReadString(ref offset),
                    Modifiers = ReadStringArray(ref offset),
                    NoteSpawnOffset = ReadFloat(ref offset),
                    LeftHanded = ReadBool(ref offset),
                    InitialHeight = ReadFloat(ref offset),
                    RoomRotation = ReadFloat(ref offset),
                    RoomCenter = ReadVRPosition(ref offset),
                    FailTime = ReadFloat(ref offset),
                    GameVersion = new AlmostVersion(ReadString(ref offset)),
                    PluginVersion = new Version(ReadString(ref offset)),
                    Platform = ReadString(ref offset),
                };
            }
        }

        private VRPoseGroup ReadVRPoseGroup(ref int offset) {

            return new VRPoseGroup() {
                Head = ReadVRPose(ref offset),
                Left = ReadVRPose(ref offset),
                Right = ReadVRPose(ref offset),
                FPS = ReadInt(ref offset),
                Time = ReadFloat(ref offset)
            };
        }

        private VRPose ReadVRPose(ref int offset) {

            return new VRPose() {
                Position = ReadVRPosition(ref offset),
                Rotation = ReadVRRotation(ref offset)
            };
        }

        private NoteEvent ReadNoteEvent(ref int offset) {

            return new NoteEvent() {
                NoteID = ReadNoteID(ref offset),
                EventType = (NoteEventType)ReadInt(ref offset),
                CutPoint = ReadVRPosition(ref offset),
                CutNormal = ReadVRPosition(ref offset),
                SaberDirection = ReadVRPosition(ref offset),
                SaberType = ReadInt(ref offset),
                DirectionOK = ReadBool(ref offset),
                SaberSpeed = ReadFloat(ref offset),
                CutAngle = ReadFloat(ref offset),
                CutDistanceToCenter = ReadFloat(ref offset),
                CutDirectionDeviation = ReadFloat(ref offset),
                BeforeCutRating = ReadFloat(ref offset),
                AfterCutRating = ReadFloat(ref offset),
                Time = ReadFloat(ref offset),
                UnityTimescale = ReadFloat(ref offset),
                TimeSyncTimescale = ReadFloat(ref offset)
            };
        }

        private NoteEvent ReadNoteEvent_v3(ref int offset) {

            return new NoteEvent() {
                NoteID = ReadNoteID_v3(ref offset),
                EventType = (NoteEventType)ReadInt(ref offset),
                CutPoint = ReadVRPosition(ref offset),
                CutNormal = ReadVRPosition(ref offset),
                SaberDirection = ReadVRPosition(ref offset),
                SaberType = ReadInt(ref offset),
                DirectionOK = ReadBool(ref offset),
                SaberSpeed = ReadFloat(ref offset),
                CutAngle = ReadFloat(ref offset),
                CutDistanceToCenter = ReadFloat(ref offset),
                CutDirectionDeviation = ReadFloat(ref offset),
                BeforeCutRating = ReadFloat(ref offset),
                AfterCutRating = ReadFloat(ref offset),
                Time = ReadFloat(ref offset),
                UnityTimescale = ReadFloat(ref offset),
                TimeSyncTimescale = ReadFloat(ref offset),

                TimeDeviation = ReadFloat(ref offset),
                WorldRotation = ReadVRRotation(ref offset),
                InverseWorldRotation = ReadVRRotation(ref offset),
                NoteRotation = ReadVRRotation(ref offset),
                NotePosition = ReadVRPosition(ref offset)
            };
        }

        private NoteID ReadNoteID(ref int offset) {

            return new NoteID() {
                Time = ReadFloat(ref offset),
                LineLayer = ReadInt(ref offset),
                LineIndex = ReadInt(ref offset),
                ColorType = ReadInt(ref offset),
                CutDirection = ReadInt(ref offset)
            };
        }

        private NoteID ReadNoteID_v3(ref int offset) {

            return new NoteID() {
                Time = ReadFloat(ref offset),
                LineLayer = ReadInt(ref offset),
                LineIndex = ReadInt(ref offset),
                ColorType = ReadInt(ref offset),
                CutDirection = ReadInt(ref offset),
                GameplayType = ReadInt(ref offset),
                ScoringType = ReadInt(ref offset),
                CutDirectionAngleOffset = ReadFloat(ref offset)
            };
        }

        private HeightEvent ReadHeightChange(ref int offset) {

            return new HeightEvent() {
                Height = ReadFloat(ref offset),
                Time = ReadFloat(ref offset)
            };
        }

        private ScoreEvent ReadScoreEvent(ref int offset) {

            return new ScoreEvent() {
                Score = ReadInt(ref offset),
                Time = ReadFloat(ref offset)
            };
        }

        private ScoreEvent ReadScoreEvent_v3(ref int offset) {

            return new ScoreEvent() {
                Score = ReadInt(ref offset),
                Time = ReadFloat(ref offset),
                ImmediateMaxPossibleScore = ReadInt(ref offset)
            };
        }

        private ComboEvent ReadComboEvent(ref int offset) {

            return new ComboEvent() {
                Combo = ReadInt(ref offset),
                Time = ReadFloat(ref offset)
            };
        }

        private MultiplierEvent ReadMultiplierEvent(ref int offset) {

            return new MultiplierEvent() {
                Multiplier = ReadInt(ref offset),
                NextMultiplierProgress = ReadFloat(ref offset),
                Time = ReadFloat(ref offset)
            };
        }

        private EnergyEvent ReadEnergyEvent(ref int offset) {

            return new EnergyEvent() {
                Energy = ReadFloat(ref offset),
                Time = ReadFloat(ref offset)
            };
        }

        // Lists
        private string[] ReadStringArray(ref int offset) {

            int size = ReadInt(ref offset);
            string[] value = new string[size];
            for (int i = 0; i < size; i++) {
                value[i] = ReadString(ref offset);
            }
            return value;
        }

        private List<T> ReadList<T>(ref int offset, ReadItem<T> readItem) {

            int size = ReadInt(ref offset);
            List<T> values = new List<T>(size);
            for (int i = 0; i < size; i++) {
                values.Add(readItem(ref offset));
            }
            return values;
        }

        // Primitives
        private string ReadString(ref int offset) {

            int stringLength = BitConverter.ToInt32(_input, offset);
            string value = Encoding.UTF8.GetString(_input, offset + 4, stringLength);
            offset += stringLength + 4;
            return value;
        }

        private int ReadInt(ref int offset) {

            int value = BitConverter.ToInt32(_input, offset);
            offset += 4;
            return value;
        }

        private float ReadFloat(ref int offset) {

            float value = BitConverter.ToSingle(_input, offset);
            offset += 4;
            return value;
        }

        private bool ReadBool(ref int offset) {

            bool value = BitConverter.ToBoolean(_input, offset);
            offset += 1;
            return value;
        }

        private VRPosition ReadVRPosition(ref int offset) {

            return new VRPosition() {
                X = ReadFloat(ref offset),
                Y = ReadFloat(ref offset),
                Z = ReadFloat(ref offset)
            };
        }

        private VRRotation ReadVRRotation(ref int offset) {

            return new VRRotation() {
                X = ReadFloat(ref offset),
                Y = ReadFloat(ref offset),
                Z = ReadFloat(ref offset),
                W = ReadFloat(ref offset)
            };
        }
    }
}
