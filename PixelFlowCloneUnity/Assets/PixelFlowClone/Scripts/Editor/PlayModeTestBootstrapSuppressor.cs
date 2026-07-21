using PixelFlowClone.Core;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// While Play Mode NUnit tests run, prevent Bootstrapper from async-loading MainMenu
    /// (that race hangs <c>LoadSceneAsync(SCN_Gameplay)</c> in PlayModeTestHelpers).
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayModeTestBootstrapSuppressor
    {
        private static bool _playModeTestRunActive;

        static PlayModeTestBootstrapSuppressor()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode && _playModeTestRunActive)
            {
                BootstrapAutoLoad.Suppress = true;
                Debug.Log("[PixelFlowClone] Bootstrap auto-load suppressed for Play Mode tests.");
            }

            if (change == PlayModeStateChange.EnteredEditMode)
            {
                _playModeTestRunActive = false;
                BootstrapAutoLoad.Suppress = false;
            }
        }

        private sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                _playModeTestRunActive = LooksLikePlayModeRun(testsToRun);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                _playModeTestRunActive = false;
                BootstrapAutoLoad.Suppress = false;
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            private static bool LooksLikePlayModeRun(ITestAdaptor node)
            {
                if (node == null)
                    return false;

                string fullName = node.FullName ?? string.Empty;
                string name = node.Name ?? string.Empty;
                if (fullName.IndexOf("PlayMode", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("PlayMode", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullName.IndexOf("PoolStress", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullName.IndexOf("LevelLoadTests", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullName.IndexOf("ConveyorMovementTests", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (!node.HasChildren || node.Children == null)
                    return false;

                foreach (ITestAdaptor child in node.Children)
                {
                    if (LooksLikePlayModeRun(child))
                        return true;
                }

                return false;
            }
        }
    }
}
