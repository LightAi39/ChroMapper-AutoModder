using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ChroMapper_LightModding.Helpers
{
    // The Analyzer parity model runs on ONNX Runtime, which needs its native library. It is shipped in Unity's native
    // plugin folder (ChroMapper_Data/Plugins/x86_64 on Windows, ChroMapper_Data/Plugins on Linux, Contents/Plugins on
    // macOS), since ChroMapper loads every dll of its Plugins folder as a managed assembly. On Windows it is loaded here
    // by its full path, and the DllImport of
    // ONNX Runtime then uses the already loaded library. On Linux and macOS, Unity resolves it from its plugin folder,
    // it is only loaded here to report a missing or broken library.
    internal static class OnnxRuntimeLoader
    {
        private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x8;
        private const int RTLD_NOW = 2;

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryExW(string path, IntPtr file, uint flags);

        [DllImport("libdl.so.2", EntryPoint = "dlopen")]
        private static extern IntPtr DlopenLinux(string path, int flags);

        [DllImport("/usr/lib/libSystem.dylib", EntryPoint = "dlopen")]
        private static extern IntPtr DlopenMac(string path, int flags);

        public static void Load()
        {
            string[] candidates;
            Func<string, bool> load;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                candidates = new[] { Path.Combine(Application.dataPath, "Plugins", "x86_64", "onnxruntime.dll") };
                load = path => LoadLibraryExW(path, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH) != IntPtr.Zero;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                candidates = new[]
                {
                    Path.Combine(Application.dataPath, "Plugins", "libonnxruntime.dylib"),
                    Path.Combine(Application.dataPath, "PlugIns", "libonnxruntime.dylib")
                };
                load = path => DlopenMac(path, RTLD_NOW) != IntPtr.Zero;
            }
            else
            {
                candidates = new[]
                {
                    Path.Combine(Application.dataPath, "Plugins", "x86_64", "libonnxruntime.so"),
                    Path.Combine(Application.dataPath, "Plugins", "libonnxruntime.so")
                };
                load = path => DlopenLinux(path, RTLD_NOW) != IntPtr.Zero;
            }

            var found = candidates.FirstOrDefault(File.Exists);
            if (found == null)
            {
                Debug.LogError($"AutoModder: ONNX Runtime is missing ({string.Join(" or ", candidates)}), the difficulties can't be rated.");
                return;
            }

            try
            {
                if (!load(found))
                {
                    Debug.LogError($"AutoModder: could not load {found}, the difficulties can't be rated.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"AutoModder: could not load {found} ({ex.Message}), the difficulties can't be rated.");
            }
        }
    }
}
