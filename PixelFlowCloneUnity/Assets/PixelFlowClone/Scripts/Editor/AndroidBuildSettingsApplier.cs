using UnityEditor;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// P4-16: applies mobile Android player settings (portrait, package, SDK, arches, IL2CPP).
    /// Includes x86_64 so Android Studio x86 emulators can run the APK without ARM translation.
    /// Re-run from menu if Project Settings drift.
    /// </summary>
    public static class AndroidBuildSettingsApplier
    {
        private const string AndroidPackage = "com.pixelflowclone.game";
        private const int MinSdk = 23;
        private const int TargetSdk = 34;

        [MenuItem("PixelFlowClone/Apply Android Build Settings (P4-16)")]
        public static void Apply()
        {
            PlayerSettings.companyName = "PixelFlowClone";
            PlayerSettings.productName = "PixelFlowClone";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.useAnimatedAutorotation = false;

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidPackage);
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinSdk;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetSdk;

            // ARMv7+ARM64 for devices; x86_64 for Android Studio emulators.
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARMv7 |
                AndroidArchitecture.ARM64 |
                AndroidArchitecture.X86_64;

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

            // Android rejects Active Input Handling = Both (0=Old, 1=Input System, 2=Both).
            SetActiveInputHandler(1);

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[PixelFlowClone] P4-16 Android settings applied: " +
                $"Portrait, package={AndroidPackage}, minSdk={MinSdk}, targetSdk={TargetSdk}, " +
                "ARMv7+ARM64+x86_64, IL2CPP, Input System Only.");
        }

        /// <summary>
        /// Unity 2022.3 exposes this via ProjectSettings serialization, not a public PlayerSettings API.
        /// 0 = Input Manager, 1 = Input System Package, 2 = Both.
        /// </summary>
        private static void SetActiveInputHandler(int value)
        {
            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0 || assets[0] == null)
            {
                Debug.LogWarning("[PixelFlowClone] Could not load ProjectSettings.asset.");
                return;
            }

            var playerSettings = new SerializedObject(assets[0]);
            SerializedProperty prop = playerSettings.FindProperty("activeInputHandler");
            if (prop == null)
            {
                Debug.LogWarning("[PixelFlowClone] Could not find activeInputHandler property.");
                return;
            }

            prop.intValue = value;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("PixelFlowClone/Use Android Mono Scripting (faster iterate)")]
        public static void UseMonoBackend()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
            AssetDatabase.SaveAssets();
            Debug.Log("[PixelFlowClone] Android scripting backend → Mono (faster Editor iterate).");
        }
    }
}
