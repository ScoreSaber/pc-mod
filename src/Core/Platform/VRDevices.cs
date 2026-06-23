using System;
using System.Collections.Generic;
using UnityEngine.XR;

namespace ScoreSaber.Core.Platform {
    internal static class VRDevices {
#if BEAT_SABER_1_29_0
        // 1.29 uses legacy VR plugins, not OpenXR
        internal static string GetDeviceHMD() {
#pragma warning disable CS0618 // Type or member is obsolete
            string str = "(xrdevice):" + XRDevice.model;
#pragma warning restore CS0618 // Type or member is obsolete
            if (SteamSettings.HMDName != null)
                str = $"{str}:(steamcfg):{SteamSettings.HMDName}";
            return "legacy:" + str;
        }

        internal static string GetDeviceControllerLeft() {
            return GetLegacyDeviceController(XRNode.LeftHand, InputDeviceCharacteristics.Left);
        }

        internal static string GetDeviceControllerRight() {
            return GetLegacyDeviceController(XRNode.RightHand, InputDeviceCharacteristics.Right);
        }

        private static string GetLegacyDeviceController(XRNode node, InputDeviceCharacteristics hand) {
            List<InputDevice> inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | hand, inputDevices);
            string device = inputDevices.Count > 0 ? "(inputdevice):" + inputDevices[0].name : string.Empty;
            string deviceName = GetDeviceName(node);
            if (deviceName != null)
                device = string.IsNullOrEmpty(device) ? deviceName : $"{device}:{deviceName}";
            return !string.IsNullOrEmpty(device) ? "legacy:" + device : "legacy:unknown";
        }
#else
        internal static string GetDeviceHMD() {

            string currentRuntime = UnityEngine.XR.OpenXR.OpenXRRuntime.name ?? string.Empty;

            var HMD = GetDeviceName(XRNode.Head);

            if (currentRuntime.IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0) {
                if (SteamSettings.HMDName != null)
                    HMD = $"{HMD}:(steamcfg):{SteamSettings.HMDName}";
            }

            if (OpenXRManager.HMDName != null)
                HMD = $"{HMD}:(openxr):{OpenXRManager.HMDName}";

            return $"{currentRuntime}:{HMD}";
        }

        internal static string GetDeviceControllerLeft() {
            return $"{UnityEngine.XR.OpenXR.OpenXRRuntime.name}:{GetDeviceName(XRNode.LeftHand)}";
        }

        internal static string GetDeviceControllerRight() {
            return $"{UnityEngine.XR.OpenXR.OpenXRRuntime.name}:{GetDeviceName(XRNode.RightHand)}";
        }
#endif

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
