using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;

namespace FireReady.EditorDiagnostics
{
    /// <summary>
    /// Writes compiler errors and warnings from our own assemblies to
    /// Logs/FireReadyCompile.log so they can be read outside the editor.
    /// </summary>
    [InitializeOnLoad]
    static class CompileLogger
    {
        const string LogPath = "Logs/FireReadyCompile.log";

        static CompileLogger()
        {
            CompilationPipeline.compilationStarted += _ => Write("--- compile started");
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyFinished;
            CompilationPipeline.compilationFinished += _ => Write("--- compile finished");
        }

        static void OnAssemblyFinished(string assemblyPath, CompilerMessage[] messages)
        {
            var name = Path.GetFileNameWithoutExtension(assemblyPath);
            int errors = 0;
            var sb = new StringBuilder();
            foreach (var m in messages)
            {
                if (m.type == CompilerMessageType.Error) errors++;
                bool ours = m.file != null && m.file.Replace('\\', '/').Contains("Assets/_Project");
                if (m.type == CompilerMessageType.Error || ours)
                    sb.AppendLine($"  {m.type}: {m.file}({m.line},{m.column}): {m.message}");
            }
            if (errors > 0 || sb.Length > 0 || name.StartsWith("FireReady") || name.StartsWith("Assembly-CSharp"))
                Write($"{name}: {errors} error(s)\n{sb}".TrimEnd());
        }

        static void Write(string text)
        {
            try
            {
                Directory.CreateDirectory("Logs");
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {text}\n");
            }
            catch (Exception) { }
        }
    }
}
