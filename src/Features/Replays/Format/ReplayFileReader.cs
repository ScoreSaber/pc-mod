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
        internal int extensions;
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
        private const int ExtensionMagic = 0x31585353; // SSX1
        private const int ExtensionTableVersion = 1;
        private const string PlaySettingsExtension = "scoresaber.play-settings";
        private const string PauseEventsExtension = "scoresaber.pause-events";
        private const string WallEventsExtension = "scoresaber.wall-events";
        private const string ControllerOffsetsExtension = "scoresaber.controller-offsets";
        private const string HsvConfigExtension = "scoresaber.hsv-config";

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
            var replay = new ReplayFile() {
                metadata = metadata,
                poseKeyframes = ReadList(ref pointers.poseKeyframes, ReadVRPoseGroup),
                heightKeyframes = ReadList(ref pointers.heightKeyframes, ReadHeightChange),
                noteKeyframes = usesV2Events ? ReadList(ref pointers.noteKeyframes, ReadNoteEvent) : ReadList(ref pointers.noteKeyframes, ReadNoteEvent_v3),
                scoreKeyframes = usesV2Events ? ReadList(ref pointers.scoreKeyframes, ReadScoreEvent) : ReadList(ref pointers.scoreKeyframes, ReadScoreEvent_v3),
                comboKeyframes = ReadList(ref pointers.comboKeyframes, ReadComboEvent),
                multiplierKeyframes = ReadList(ref pointers.multiplierKeyframes, ReadMultiplierEvent),
                energyKeyframes = ReadList(ref pointers.energyKeyframes, ReadEnergyEvent)
            };
            ReadExtensions(replay, pointers.extensions);
            return replay;
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
                extensions = ReadInt(ref offset)
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

        private void ReadExtensions(ReplayFile replay, int offset) {

            if (offset <= 0 || offset >= _input.Length) {
                return;
            }

            try {
                if (ReadInt(ref offset) != ExtensionMagic) {
                    return;
                }
                int tableVersion = ReadInt(ref offset);
                if (tableVersion != ExtensionTableVersion) {
                    return;
                }

                int entryCount = ReadInt(ref offset);
                for (int i = 0; i < entryCount; i++) {
                    string id = ReadString(ref offset);
                    int version = ReadInt(ref offset);
                    int payloadLength = ReadInt(ref offset);
                    if (payloadLength < 0 || offset + payloadLength > _input.Length) {
                        throw new ReplayVersionException("Replay extension payload is out of bounds");
                    }

                    int payloadOffset = offset;
                    int nextOffset = offset + payloadLength;
                    if (id == PlaySettingsExtension && version == 1) {
                        ReadPlaySettings(replay, ref payloadOffset);
                    } else if (id == PauseEventsExtension && version == 1) {
                        replay.pauseKeyframes = ReadList(ref payloadOffset, ReadPauseEvent);
                    } else if (id == WallEventsExtension && version == 1) {
                        replay.wallKeyframes = ReadList(ref payloadOffset, ReadWallEvent);
                    } else if (id == ControllerOffsetsExtension && version == 1) {
                        ReadControllerOffsets(replay, ref payloadOffset);
                    } else if (id == HsvConfigExtension && version == 1) {
                        replay.hsvConfig = ReadBytes(payloadOffset, payloadLength);
                    }
                    offset = nextOffset;
                }
            } catch (Exception ex) {
                Plugin.Log.Debug($"Ignoring replay extensions: {ex.Message}");
            }
        }

        private void ReadPlaySettings(ReplayFile replay, ref int offset) {

            Metadata metadata = replay.metadata;
            metadata.HasPlaySettingsExtension = true;
            metadata.SongSpeed = ReadFloat(ref offset);
            metadata.JumpDistance = ReadFloat(ref offset);
            metadata.LeftSaberColor = ReadColor(ref offset);
            metadata.RightSaberColor = ReadColor(ref offset);
            metadata.ObstacleColor = ReadColor(ref offset);
            metadata.EnvironmentColor0 = ReadColor(ref offset);
            metadata.EnvironmentColor1 = ReadColor(ref offset);
            metadata.EnvironmentColorW = ReadColor(ref offset);
            metadata.EnvironmentColor0Boost = ReadColor(ref offset);
            metadata.EnvironmentColor1Boost = ReadColor(ref offset);
            metadata.EnvironmentColorWBoost = ReadColor(ref offset);
            metadata.SupportsEnvironmentColorBoost = ReadBool(ref offset);
            string environment = ReadString(ref offset);
            if (!string.IsNullOrEmpty(environment)) {
                metadata.Environment = environment;
            }
            metadata.EnvironmentEffectsFilterDefaultPreset = ReadInt(ref offset);
            metadata.EnvironmentEffectsFilterExpertPlusPreset = ReadInt(ref offset);
            metadata.EnvironmentEffectsFilterPreset = ReadInt(ref offset);
            metadata.NoTextsAndHuds = ReadBool(ref offset);
            metadata.SaberTrailIntensity = ReadFloat(ref offset);
            metadata.HideNoteSpawnEffect = ReadBool(ref offset);
            metadata.ArcsHapticFeedback = ReadBool(ref offset);
            metadata.ArcVisibility = ReadInt(ref offset);
            replay.metadata = metadata;
        }

        private void ReadControllerOffsets(ReplayFile replay, ref int offset) {

            Metadata metadata = replay.metadata;
            metadata.ControllerOffsets = new ReplayControllerOffsets() {
                Shared = ReadControllerOffset(ref offset),
                Left = ReadControllerOffset(ref offset),
                Right = ReadControllerOffset(ref offset)
            };
            replay.metadata = metadata;
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

        private PauseEvent ReadPauseEvent(ref int offset) {

            return new PauseEvent() {
                Time = ReadFloat(ref offset),
                Duration = ReadLong(ref offset),
                UnixStartTime = ReadLong(ref offset),
                UnixEndTime = ReadLong(ref offset)
            };
        }

        private WallEvent ReadWallEvent(ref int offset) {

            return new WallEvent() {
                Time = ReadFloat(ref offset),
                ExitTime = ReadFloat(ref offset),
                Energy = ReadFloat(ref offset),
                ObstacleTime = ReadFloat(ref offset),
                ObstacleDuration = ReadFloat(ref offset),
                LineIndex = ReadInt(ref offset),
                LineLayer = ReadInt(ref offset),
                Width = ReadInt(ref offset),
                Height = ReadInt(ref offset)
            };
        }

        private ReplayControllerOffset? ReadControllerOffset(ref int offset) {

            if (!ReadBool(ref offset)) {
                return null;
            }

            return new ReplayControllerOffset() {
                Position = ReadVRPosition(ref offset),
                Rotation = ReadVRPosition(ref offset)
            };
        }

        private byte[] ReadBytes(int offset, int length) {

            var bytes = new byte[length];
            Buffer.BlockCopy(_input, offset, bytes, 0, length);
            return bytes;
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

        private long ReadLong(ref int offset) {

            long value = BitConverter.ToInt64(_input, offset);
            offset += 8;
            return value;
        }

        private UnityEngine.Color? ReadColor(ref int offset) {

            if (!ReadBool(ref offset)) {
                return null;
            }

            return new UnityEngine.Color(
                ReadFloat(ref offset),
                ReadFloat(ref offset),
                ReadFloat(ref offset),
                ReadFloat(ref offset));
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
