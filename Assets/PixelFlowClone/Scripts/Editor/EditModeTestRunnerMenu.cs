using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// P4-18: one-click Edit Mode test run from the menu.
    /// </summary>
    public static class EditModeTestRunnerMenu
    {
        [MenuItem("PixelFlowClone/Run All Edit Mode Tests (P4-18)")]
        public static void RunAllEditModeTests()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "PixelFlowClone.Tests" }
            };

            api.RegisterCallbacks(new EditModeTestCallbacks());
            api.Execute(new ExecutionSettings(filter));
            Debug.Log("[PixelFlowClone] P4-18: Edit Mode tests started — watch Test Runner / Console.");
        }

        private sealed class EditModeTestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"[PixelFlowClone] P4-18 EditMode run started ({testsToRun.TestCaseCount} cases).");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                int pass = Count(result, TestStatus.Passed);
                int fail = Count(result, TestStatus.Failed);
                int skip = Count(result, TestStatus.Skipped) + Count(result, TestStatus.Inconclusive);

                if (fail == 0)
                {
                    Debug.Log(
                        $"[PixelFlowClone] P4-18 PASS — Edit Mode all green " +
                        $"(passed={pass}, skipped={skip}).");
                }
                else
                {
                    Debug.LogError(
                        $"[PixelFlowClone] P4-18 FAIL — Edit Mode failures " +
                        $"(passed={pass}, failed={fail}, skipped={skip}). " +
                        "Open Window → General → Test Runner → EditMode for details.");
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                    Debug.LogError($"[PixelFlowClone] FAIL: {result.Name}\n{result.Message}");
            }

            private static int Count(ITestResultAdaptor node, TestStatus status)
            {
                int n = 0;
                Walk(node, status, ref n);
                return n;
            }

            private static void Walk(ITestResultAdaptor node, TestStatus status, ref int n)
            {
                if (node == null)
                    return;

                if (!node.HasChildren && node.TestStatus == status)
                    n++;

                if (node.Children == null)
                    return;

                foreach (ITestResultAdaptor child in node.Children)
                    Walk(child, status, ref n);
            }
        }
    }
}
