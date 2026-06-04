using ScoreSaber.Features.Replays.Format;
using System.IO;
using UnityEngine;

namespace ScoreSaber.Features.Replays {
    internal static class ReplayExtensions {
        internal static VRPosition Convert(this Vector3 vec) {
            return new VRPosition { X = vec.x, Y = vec.y, Z = vec.z };
        }

        internal static VRRotation Convert(this Quaternion quat) {
            return new VRRotation { X = quat.x, Y = quat.y, Z = quat.z, W = quat.w };
        }

        internal static VRPose Convert(this Transform transform) {
            return new VRPose { Position = transform.position.Convert(), Rotation = transform.rotation.Convert() };
        }

        internal static Vector3 Convert(this VRPosition vec) {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        internal static Quaternion Convert(this VRRotation quat) {
            return new Quaternion(quat.X, quat.Y, quat.Z, quat.W);
        }

        internal static string Truncate(this string value, int maxLength) {

            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        internal static string ReplaceInvalidChars(this string filename) {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
