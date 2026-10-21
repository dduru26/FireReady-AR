using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace FireReady.EditorTools
{
    /// <summary>Runs the edit-mode tests and writes the results to Logs/FireReadyTests.log.</summary>
    internal static class TestRunnerMenu
    {
        const string LogPath = "Logs/FireReadyTests.log";

        [MenuItem("Tools/FireReady/Run Edit Mode Tests")]
        static void Run()
        {
            File.WriteAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] run started\n");
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "FireReady.Tests.EditMode" }
            }));
        }

        sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result) =>
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] RUN FINISHED: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped\n");

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren) return;
                File.AppendAllText(LogPath, $"  {result.ResultState}: {result.Test.FullName}{(string.IsNullOrEmpty(result.Message) ? "" : " - " + result.Message.Trim())}\n");
            }
        }
    }
}
