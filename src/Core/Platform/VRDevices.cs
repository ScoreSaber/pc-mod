using System.Collections.Generic;
using UnityEngine.XR;

namespace ScoreSaber.Core.Platform {
    internal static class VRDevices {
        internal static string GetDeviceHMD() {

            var currentRuntime = UnityEngine.XR.OpenXR.OpenXRRuntime.name.ToLower();

            var HMD = GetDeviceName(XRNode.Head);

            if (currentRuntime.Contains("steam")) {
                if (SteamSettings.HMDName != null)
                    HMD = $"{HMD}:(steamcfg):{SteamSettings.HMDName}";
            }

            if (OpenXRManager.HMDName != null)
                HMD = $"{HMD}:(openxr):{OpenXRManager.HMDName}";

            return $"{UnityEngine.XR.OpenXR.OpenXRRuntime.name}:{HMD}";
        }

        internal static string GetDeviceControllerLeft() {
            return $"{UnityEngine.XR.OpenXR.OpenXRRuntime.name}:{GetDeviceName(XRNode.LeftHand)}";
        }

        internal static string GetDeviceControllerRight() {
            return $"{UnityEngine.XR.OpenXR.OpenXRRuntime.name}:{GetDeviceName(XRNode.RightHand)}";
        }

        private static string GetDeviceName(XRNode node) {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(node, devices);
            if (devices.Count == 0) {
                return null;
            }
            return "(xrnode):" + devices[0].name;
        }

        internal static string GetLegacyHMDFriendlyName(int HMD) {

            if (HMD == 0) { return "Unknown"; }
            if (HMD == 1) { return "Oculus Rift CV1"; }
            if (HMD == 2) { return "HTC VIVE"; }
            if (HMD == 4) { return "HTC VIVE Pro"; }
            if (HMD == 8) { return "Windows Mixed Reality"; }
            if (HMD == 16) { return "Oculus Rift S"; }
            if (HMD == 32) { return "Oculus Quest"; }
            if (HMD == 64) { return "Valve Index"; }
            if (HMD == 128) { return "HTC VIVE Cosmos"; }
            return "Unknown";
        }
    }
}
