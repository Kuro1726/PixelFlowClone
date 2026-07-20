using System;
using System.IO;
using System.Linq;
using PixelFlowClone.Utils;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// P4-17: WebGL player settings + smoke build. Also hosts Windows EXE offline build.
    /// </summary>
    public static class WebGLBuildSmoke
    {
        private const string SmokeOutputFolder = "Builds/WebGL_Smoke";

        [MenuItem("PixelFlowClone/Apply WebGL Build Settings (P4-17)")]
        public static void ApplySettings()
        {
            PlayerSettings.companyName = "PixelFlowClone";
            PlayerSettings.productName = "PixelFlowClone";
            PlayerSettings.bundleVersion = "1.0";

            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithoutStacktrace;
            PlayerSettings.WebGL.memorySize = 64;
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[PixelFlowClone] P4-17 WebGL settings applied: canvas 540x960, " +
                "compression=Disabled (smoke), decompressionFallback=on, memory=64MB.");
        }

        [MenuItem("PixelFlowClone/Build WebGL Smoke (P4-17)", priority = 20)]
        public static void BuildSmoke()
        {
            string modulePath = GetWebGLSupportPath();
            if (string.IsNullOrEmpty(modulePath))
            {
                ShowWebGLMissingDialog();
                return;
            }

            ApplySettings();

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog(
                    "Cannot switch to WebGL",
                    "WebGLSupport folder exists but Unity could not activate WebGL.\n\n" +
                    "1) Close Unity completely\n" +
                    "2) Re-open this project with 2022.3.62f3\n" +
                    "3) Try Build WebGL Smoke again\n\n" +
                    "Or use: PixelFlowClone → Build Windows EXE (Offline)",
                    "OK");
                Debug.LogError(
                    "[PixelFlowClone] SwitchActiveBuildTarget(WebGL) failed. " +
                    "Restart Unity after installing/repairing WebGL module. Module path: " + modulePath);
                return;
            }

            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[PixelFlowClone] No enabled scenes in Build Settings.");
                return;
            }

            string outputPath = ResolveOutputPath(SmokeOutputFolder);
            if (outputPath == null)
                return;

            Directory.CreateDirectory(outputPath);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            };

            Debug.Log($"[PixelFlowClone] Starting WebGL smoke build → {outputPath} (module: {modulePath})");

            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[PixelFlowClone] WebGL build exception: " + ex.Message + "\n" +
                    "Restart Unity after installing WebGL, or use Build Windows EXE (Offline).");
                ShowWebGLMissingDialog();
                return;
            }

            if (report == null)
            {
                ShowWebGLMissingDialog();
                return;
            }

            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(
                    $"[PixelFlowClone] P4-17 WebGL smoke BUILD OK ({summary.totalSize / (1024 * 1024)} MB). " +
                    "Serve with local HTTP server (not file://), open index.html, click waiting pig.");
                EditorUtility.RevealInFinder(outputPath);
                return;
            }

            Debug.LogError(
                $"[PixelFlowClone] P4-17 WebGL smoke BUILD FAILED: {summary.result}. " +
                "If you just installed WebGL, fully quit Unity and reopen the project.");
            ShowWebGLMissingDialog();
        }

        [MenuItem("PixelFlowClone/Build WebGL Smoke (P4-17)", validate = true)]
        private static bool ValidateBuildSmoke()
        {
            return !string.IsNullOrEmpty(GetWebGLSupportPath());
        }

        [MenuItem("PixelFlowClone/Build Windows EXE (Offline)", priority = 10)]
        public static void BuildWindowsExe()
        {
            ApplyStandalonePortraitSettings();

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                Debug.LogWarning(
                    "[PixelFlowClone] Could not switch active target to StandaloneWindows64 — trying build anyway.");
            }

            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[PixelFlowClone] No enabled scenes in Build Settings.");
                return;
            }

            string folder = ResolveOutputPath("Builds/Windows");
            if (folder == null)
                return;

            Directory.CreateDirectory(folder);
            string exePath = Path.Combine(folder, PlayerSettings.productName + ".exe");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            Debug.Log($"[PixelFlowClone] Starting Windows EXE build → {exePath}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(
                    "[PixelFlowClone] Windows EXE build OK (portrait 540x960 window). " +
                    "Copy the whole Builds/Windows folder (exe + _Data), then run the .exe offline.");
                EditorUtility.RevealInFinder(folder);
            }
            else
            {
                Debug.LogError($"[PixelFlowClone] Windows EXE build FAILED: {summary.result}");
            }
        }

        [MenuItem("PixelFlowClone/Apply Standalone Portrait Window")]
        public static void ApplyStandalonePortraitSettings()
        {
            PlayerSettings.defaultScreenWidth = PortraitDisplay.Width;
            PlayerSettings.defaultScreenHeight = PortraitDisplay.Height;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.allowFullscreenSwitch = false;
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[PixelFlowClone] Standalone portrait window {PortraitDisplay.Width}x{PortraitDisplay.Height} applied.");
        }

        private static string GetWebGLSupportPath()
        {
            string editorData = EditorApplication.applicationContentsPath;
            string primary = Path.Combine(editorData, "PlaybackEngines", "WebGLSupport");
            if (Directory.Exists(primary))
                return primary;

            string parent = Directory.GetParent(editorData)?.FullName;
            if (!string.IsNullOrEmpty(parent))
            {
                string alt = Path.Combine(parent, "PlaybackEngines", "WebGLSupport");
                if (Directory.Exists(alt))
                    return alt;
            }

            return null;
        }

        private static void ShowWebGLMissingDialog()
        {
            EditorUtility.DisplayDialog(
                "WebGL build không chạy được",
                "Unity báo WebGL not supported.\n\n" +
                "Thử theo thứ tự:\n" +
                "1) Thoát hẳn Unity → mở lại project bằng 2022.3.62f3\n" +
                "2) Hub → Installs → 2022.3.62f3 → Add modules → WebGL Build Support (repair nếu đã tick)\n" +
                "3) File → Build Settings → chọn WebGL → Switch Platform, rồi build lại\n\n" +
                "Chơi offline trên laptop (không cần WebGL):\n" +
                "PixelFlowClone → Build Windows EXE (Offline)",
                "OK");

            Debug.LogError(
                "[PixelFlowClone] WebGL build unavailable. Restart Unity / repair WebGL module, " +
                "or use Build Windows EXE (Offline).");
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToArray();
        }

        private static string ResolveOutputPath(string relativeFolder)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("[PixelFlowClone] Could not resolve project root.");
                return null;
            }

            return Path.Combine(projectRoot, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
