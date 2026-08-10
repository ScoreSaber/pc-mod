using Legato.XR;
using System;
using UnityEngine.XR;

namespace ScoreSaber.Core.Platform {
    internal static class VRDevices {
#if BEAT_SABER_1_29_0
        internal static string GetDeviceHMD() {
            string device = "(xrdevice):" + XrDeviceDiscovery.LegacyHeadsetModel;
            if (SteamSettings.HMDName != null)
                device = $"{device}:(steamcfg):{SteamSettings.HMDName}";
            return "legacy:" + device;
        }

        internal static string GetDeviceControllerLeft() => GetLegacyDeviceController(XRNode.LeftHand, InputDeviceCharacteristics.Left);

        internal static string GetDeviceControllerRight() => GetLegacyDeviceController(XRNode.RightHand, InputDeviceCharacteristics.Right);

        private static string GetLegacyDeviceController(XRNode node, InputDeviceCharacteristics hand) {
            string controllerName = XrDeviceDiscovery.GetControllerDeviceName(hand);
            string device = controllerName == null ? string.Empty : "(inputdevice):" + controllerName;
            string deviceName = GetDeviceName(node);
            if (deviceName != null)
                device = string.IsNullOrEmpty(device) ? deviceName : $"{device}:{deviceName}";
            return !string.IsNullOrEmpty(device) ? "legacy:" + device : "legacy:unknown";
        }
#else
        internal static string GetDeviceHMD() {
            string currentRuntime = XrDeviceDiscovery.RuntimeName;
            string hmd = GetDeviceName(XRNode.Head);

            if (currentRuntime.IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0 && SteamSettings.HMDName != null)
                hmd = $"{hmd}:(steamcfg):{SteamSettings.HMDName}";
            if (OpenXRManager.HMDName != null)
                hmd = $"{hmd}:(openxr):{OpenXRManager.HMDName}";

            return $"{currentRuntime}:{hmd}";
        }

        internal static string GetDeviceControllerLeft() => $"{XrDeviceDiscovery.RuntimeName}:{GetDeviceName(XRNode.LeftHand)}";

        internal static string GetDeviceControllerRight() => $"{XrDeviceDiscovery.RuntimeName}:{GetDeviceName(XRNode.RightHand)}";
#endif

        private static string GetDeviceName(XRNode node) {
            string deviceName = XrDeviceDiscovery.GetNodeDeviceName(node);
            return deviceName == null ? null : "(xrnode):" + deviceName;
        }

        internal static string GetLegacyHMDFriendlyName(int hmd) {
            if (hmd == 1) return "Oculus Rift CV1";
            if (hmd == 2) return "HTC VIVE";
            if (hmd == 4) return "HTC VIVE Pro";
            if (hmd == 8) return "Windows Mixed Reality";
            if (hmd == 16) return "Oculus Rift S";
            if (hmd == 32) return "Oculus Quest";
            if (hmd == 64) return "Valve Index";
            if (hmd == 128) return "HTC VIVE Cosmos";
            return "Unknown";
        }
    }
}
