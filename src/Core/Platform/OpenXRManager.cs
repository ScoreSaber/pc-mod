using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

// Adapted from https://forum.unity.com/threads/openxr-is-it-no-longer-possible-to-get-descriptive-device-names.1051493/#post-8316300

namespace ScoreSaber.Core.Platform {
    internal static unsafe class OpenXRManager {
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, ulong lpBaseAddress, byte[] lpBuffer, int dwSize, int lpNumberOfBytesRead = 0);

        [DllImport("openxr_loader", EntryPoint = "xrGetSystemProperties", CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong GetSystemProperties(long* instance, ulong systemId, XrSystemProperties* properties);

        [DllImport("openxr_loader", EntryPoint = "xrGetSystem", CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong GetSystem(long* instance, XrSystemGetInfo* getInfo, ulong* systemId);

        // delegate for xrGetInstanceProcAddr() https://registry.khronos.org/OpenXR/specs/1.0/man/html/xrGetInstanceProcAddr.html
        private delegate ulong xrGetInstanceProcAddrDelegate(ulong instance, StringBuilder name, IntPtr* function);

        // delegate for xrGetSystemProperties() https://registry.khronos.org/OpenXR/specs/1.0/man/html/xrGetSystemProperties.html
        private delegate ulong xrGetSystemPropertiesDelegate(ulong instance, ulong systemId, void* properties);

        // delegate for xrGetSystemProperties() https://registry.khronos.org/OpenXR/specs/1.0/man/html/xrGetSystem.html
        private delegate ulong xrGetSystemDelegate(ulong instance, XrSystemGetInfo* getInfo, ulong* systemId);

        private delegate ulong xrGetCurrentInstanceDelegate();

        // XrSystemProperties structure https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrSystemProperties.html
        private struct XrSystemProperties {
            public ulong type;     // 64 bit
            public void* next;     // 64 bit
            public ulong systemId; // 64 bit
            public int vendorId; // 32 bit
            public fixed byte systemName[XR_MAX_SYSTEM_NAME_SIZE];
            public XrSystemGraphicsProperties graphicsProperties;
            public XrSystemTrackingProperties trackingProperties;
        };

        // XrSystemGraphicsProperties structure https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrSystemGraphicsProperties.html
        private struct XrSystemGraphicsProperties {
            public uint maxSwapchainImageHeight;
            public uint maxSwapchainImageWidth;
            public uint maxLayerCount;
        };

        // XrSystemTrackingProperties structure https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrSystemTrackingProperties.html
        private struct XrSystemTrackingProperties {
            public uint orientationTracking;
            public uint positionTracking;
        };

        // XrSystemGetInfo structure https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrSystemGetInfo.html
        private struct XrSystemGetInfo {
            public ulong type;       // 64 bit
            public void* next;       // 64 bit
            public ulong formFactor; // 64 bit
        }

        // https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrFormFactor.html
        private const int XR_FORM_FACTOR_HEAD_MOUNTED_DISPLAY = 1;

        // https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrStructureType.html
        private const int XR_TYPE_SYSTEM_GET_INFO = 4;
        private const int XR_TYPE_SYSTEM_PROPERTIES = 5;
        private const int XR_MAX_SYSTEM_NAME_SIZE = 256;

        internal static string HMDName = null;

        // 1.40.9 rebuilt openxr_loader.dll, so scan for both patterns
        internal static readonly string[] xrGetCurrentInstancePatterns = {
            "48 83 EC 28 65 48 8B 04 25 ? ? ? ? 8B 0D ? ? ? ? BA ? ? ? ? 48 8B 0C C8 8B 04 0A 39 05 ? ? ? ? 7F 0C 48 8D 05 ? ? ? ? 48 83 C4 28 C3 48 8D 0D ? ? ? ?",
            "48 83 EC 28 8B 0D ? ? ? ? 65 48 8B 04 25 ? ? ? ? BA ? ? ? ? 48 8B 04 C8 8B 04 02 39 05 ? ? ? ? 7F 0C 48 8D 05 ? ? ? ? 48 83 C4 28 C3 48 8D 0D ? ? ? ?"
        };

        internal static void Initialize() {
            HMDName = AttemptGetHMD();
        }

        private static string AttemptGetHMD() {
            try {
                var currentProcess = Process.GetCurrentProcess();
                var openXRLoaderModule = currentProcess.Modules.Cast<ProcessModule>().FirstOrDefault(module => module.ModuleName == "openxr_loader.dll");

                if (openXRLoaderModule == null) {
                    throw new Exception("openxr_loader not found");
                }

                // Get our xrInstance
                var xrGetCurrentInstanceOffset = IntPtr.Zero;
                foreach (var pattern in xrGetCurrentInstancePatterns) {
                    xrGetCurrentInstanceOffset = PatternScan(Process.GetCurrentProcess(), openXRLoaderModule, pattern);
                    if (xrGetCurrentInstanceOffset != IntPtr.Zero) {
                        break;
                    }
                }

                if (xrGetCurrentInstanceOffset == IntPtr.Zero) {
                    throw new Exception("xrGetCurrentInstance not found");
                }

                var xrGetCurrentInstance = Marshal.GetDelegateForFunctionPointer<xrGetCurrentInstanceDelegate>(xrGetCurrentInstanceOffset);
                var xrCurrentInstance = new IntPtr(*(long*)xrGetCurrentInstance());
                var xrInstance = (long*)*(ulong*)new IntPtr((byte*)xrCurrentInstance.ToPointer() + 8);

                // Get our systemId
                var info = new XrSystemGetInfo {
                    type = XR_TYPE_SYSTEM_GET_INFO,
                    formFactor = XR_FORM_FACTOR_HEAD_MOUNTED_DISPLAY
                };

                ulong systemId = 0;
                var getSystemResult = GetSystem(xrInstance, &info, &systemId);

                if (getSystemResult != 0) {
                    Plugin.Log.Info($"Failed to get system from OpenXR {getSystemResult}");
                    return null;
                }

                // Get our system properties
                XrSystemProperties properties;
                properties.type = XR_TYPE_SYSTEM_PROPERTIES;
                var result = GetSystemProperties(xrInstance, systemId, &properties);

                if (result != 0) {
                    Plugin.Log.Info($"Failed to get system properties from OpenXR {result}");
                    return null;
                }

                var systemNameStrBuilder = new StringBuilder();

                for (int charIndex = 0; charIndex < XR_MAX_SYSTEM_NAME_SIZE; charIndex++) {
                    if (properties.systemName[charIndex] == 0) {
                        break;
                    }

                    systemNameStrBuilder.Append(((char)properties.systemName[charIndex]));
                }
                return systemNameStrBuilder.ToString();
            } catch (Exception ex) {
                Plugin.Log.Info($"Failed to get HMD from OpenXR {ex}");
                return null;
            }
        }


        static IntPtr PatternScan(Process process, ProcessModule module, string pattern) {

            byte[] moduleBuffer = new byte[module.ModuleMemorySize];
            var success = ReadProcessMemory(process.Handle, (ulong)module.BaseAddress, moduleBuffer, module.ModuleMemorySize);

            if (!success) {
                throw new Exception("Failed to read process memory");
            }

            // Convert the input pattern string into a list of bytes
            List<byte> patternBytes = new List<byte>();
            foreach (var b in pattern.Split(' ')) {
                // Treat '?' as a wildcard (0x0) and convert other hex values to bytes
                patternBytes.Add(b == "?" ? (byte)0x0 : Convert.ToByte(b, 16));
            }

            for (int i = 0; i < moduleBuffer.Length - patternBytes.Count + 1; i++) {
                // Flag indicating whether the pattern is matched at the current position
                bool isPatternMatch = true;

                for (int j = 0; j < patternBytes.Count; j++) {
                    if (patternBytes[j] != 0x0 && patternBytes[j] != moduleBuffer[i + j]) {
                        // If the bytes don't match, set the flag to false and break out of the loop
                        isPatternMatch = false;
                        break;
                    }
                }

                // If the pattern is fully matched, return the address of the match
                if (isPatternMatch) {
                    return IntPtr.Add(module.BaseAddress, i);
                }
            }

            return IntPtr.Zero;
        }
    }
}
