using System;
using System.Collections.Generic;
using UnityEngine;
#pragma warning disable IDE1006 // Naming Styles
namespace ScoreSaber.Features.Replays.Format {
    internal class ReplayFile {
        internal Metadata metadata;
        internal List<VRPoseGroup> poseKeyframes;
        internal List<HeightEvent> heightKeyframes;
        internal List<NoteEvent> noteKeyframes;
        internal List<ScoreEvent> scoreKeyframes;
        internal List<ComboEvent> comboKeyframes;
        internal List<MultiplierEvent> multiplierKeyframes;
        internal List<EnergyEvent> energyKeyframes;

        internal ReplayFile() {

            poseKeyframes = new List<VRPoseGroup>();
            heightKeyframes = new List<HeightEvent>();
            noteKeyframes = new List<NoteEvent>();
            scoreKeyframes = new List<ScoreEvent>();
            comboKeyframes = new List<ComboEvent>();
            multiplierKeyframes = new List<MultiplierEvent>();
            energyKeyframes = new List<EnergyEvent>();
        }

        internal void Mirror() {
            for (int i = 0; i < poseKeyframes.Count; ++i) {
                var keyframe = poseKeyframes[i];
                keyframe.Mirror();
                poseKeyframes[i] = keyframe;
            }
            for (int i = 0; i < noteKeyframes.Count; ++i) {
                var keyframe = noteKeyframes[i];
                keyframe.Mirror();
                noteKeyframes[i] = keyframe;
            }
            metadata.LeftHanded = !metadata.LeftHanded;
        }
    }

    internal struct Metadata {
        internal Version Version;
        internal string LevelID;
        internal int Difficulty;
        internal string Characteristic;
        internal string Environment;
        internal string[] Modifiers;
        internal float NoteSpawnOffset;
        internal bool LeftHanded;
        internal float InitialHeight;
        internal float RoomRotation;
        internal VRPosition RoomCenter;
        internal float FailTime;
        internal Hive.Versioning.Version GameVersion;
        internal Version PluginVersion;
        internal string Platform; // Quest or PC
        internal float SongSpeed;
        internal float JumpDistance;
        internal Color? LeftSaberColor;
        internal Color? RightSaberColor;
    };

    internal struct ScoreEvent {
        public int Score;
        public float Time;
        public int? ImmediateMaxPossibleScore;
    };

    internal struct ComboEvent {
        internal int Combo;
        internal float Time;
    };

    internal struct NoteEvent {
        internal NoteID NoteID;
        internal NoteEventType EventType;
        internal VRPosition CutPoint;
        internal VRPosition CutNormal;
        internal VRPosition SaberDirection;
        internal int SaberType;
        internal bool DirectionOK;
        internal float SaberSpeed;
        internal float CutAngle;
        internal float CutDistanceToCenter;
        internal float CutDirectionDeviation;
        internal float BeforeCutRating;
        internal float AfterCutRating;
        internal float Time;
        internal float UnityTimescale;
        internal float TimeSyncTimescale;

        // stored but not replayed
        internal float? TimeDeviation;
        internal VRRotation? WorldRotation;
        internal VRRotation? InverseWorldRotation;
        internal VRRotation? NoteRotation;
        internal VRPosition? NotePosition;

        internal void Mirror() {
            NoteID.Mirror();
            CutPoint.Mirror();
            CutNormal.Mirror();
            SaberDirection.Mirror();
            SaberType = 1 - SaberType;
            CutAngle = -CutAngle;
        }
    };

    internal enum NoteEventType {
        None,
        GoodCut,
        BadCut,
        Miss,
        Bomb
    }

    internal static class RelevantGameVersions {
        public static readonly Hive.Versioning.Version Version_1_40 = new Hive.Versioning.Version("1.40.0");
        public static readonly Hive.Versioning.Version Version_1_40_9 = new Hive.Versioning.Version("1.40.9");
    }

    internal struct NoteID : IEquatable<NoteID> {
        internal float Time;
        internal int LineLayer;
        internal int LineIndex;
        internal int ColorType;
        internal int CutDirection;
        internal int? GameplayType;
        internal int? ScoringType;
        internal float? CutDirectionAngleOffset;

        public static bool operator ==(NoteID a, NoteID b) {
            return Mathf.Approximately(a.Time, b.Time) && a.LineIndex == b.LineIndex && a.LineLayer == b.LineLayer && a.ColorType == b.ColorType && a.CutDirection == b.CutDirection
                && a.GameplayType == b.GameplayType && a.ScoringType == b.ScoringType && a.CutDirectionAngleOffset == b.CutDirectionAngleOffset;
        }

        public static bool operator !=(NoteID a, NoteID b) {
            return !(a == b);
        }

        public override int GetHashCode() {
            return Time.GetHashCode() ^ LineLayer ^ LineIndex;
        }

        public override bool Equals(object obj) {
            return Equals((NoteID)obj);
        }

        public bool Equals(NoteID other) {
            return this == other;
        }

        internal bool MatchesScoringType(NoteData.ScoringType comparedScoringType, Hive.Versioning.Version gameVersion) {
            if (ScoringType is int scoringType) {
                return ReplayScoringTypes.Matches(scoringType, ReplayScoringTypes.EraOf(gameVersion), comparedScoringType);
            }
            return true;
        }

        internal void Mirror() {
            LineIndex = 3 - LineIndex;
            ColorType = 1 - ColorType;
            switch (CutDirection) {
                case 2: CutDirection = 3; break;
                case 3: CutDirection = 2; break;
                case 4: CutDirection = 5; break;
                case 5: CutDirection = 4; break;
                case 6: CutDirection = 7; break;
                case 7: CutDirection = 6; break;
            }
            if (CutDirectionAngleOffset is float cutDirectionAngleOffset) {
                CutDirectionAngleOffset = -cutDirectionAngleOffset;
            }
        }
    };

    internal struct EnergyEvent {
        internal float Energy;
        internal float Time;
    };

    internal struct HeightEvent {
        internal float Height;
        internal float Time;
    };

    internal struct MultiplierEvent {
        internal int Multiplier;
        internal float NextMultiplierProgress;
        internal float Time;
    };

    internal struct VRPoseGroup {
        internal VRPose Head;
        internal VRPose Left;
        internal VRPose Right;
        internal int FPS;
        internal float Time;

        internal void Mirror() {
            Head.Mirror();

            (Right, Left) = (Left, Right);

            Left.Mirror();
            Right.Mirror();
        }
    };

    internal struct VRPose {
        internal VRPosition Position;
        internal VRRotation Rotation;

        internal void Mirror() {
            Position.Mirror();
            Rotation.Mirror();
        }
    };

    internal struct VRPosition {
        internal float X;
        internal float Y;
        internal float Z;

        internal static VRPosition None() {
            return new VRPosition() { X = 0, Y = 0, Z = 0 };
        }

        internal void Mirror() {
            X = -X;
        }
    };

    internal struct VRRotation {
        internal float X;
        internal float Y;
        internal float Z;
        internal float W;

        internal void Mirror() {
            Y = -Y;
            Z = -Z;
        }
    };
}
