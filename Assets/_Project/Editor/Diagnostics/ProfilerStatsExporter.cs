using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace FireReady.EditorDiagnostics
{
    /// <summary>
    /// Saves the frames currently in the Profiler and writes per-frame numbers
    /// (frame time, main-thread work, GC allocation, memory, draw calls) to Logs.
    /// </summary>
    public static class ProfilerStatsExporter
    {
        const string LogDir = "Logs/Profiler";

        static readonly string[] Watched =
        {
            "ExtinguisherController", "ScenarioController", "FireVisuals", "PlacementController",
            "SessionRecorder", "AudioManager", "SimulationHudView", "EffectPool"
        };

        [MenuItem("Tools/FireReady/Export Profiler Stats")]
        static void Export()
        {
            Directory.CreateDirectory(LogDir);
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var summary = new StringBuilder();
            if (first < 0 || last < first)
            {
                File.WriteAllText($"{LogDir}/summary-{stamp}.txt", "No frames in the Profiler.\n");
                return;
            }

            ProfilerDriver.SaveProfile($"{LogDir}/capture-{stamp}.data");

            var csv = new StringBuilder("frame,frameMs,mainWorkMs,waitMs,gcBytes,gcCount,totalUsedMB,gcUsedMB,batches,setPass,tris," + string.Join(",", Watched.Select(w => w + "Ms")) + "\n");
            var frameMs = new List<float>(); var work = new List<float>(); var gc = new List<long>();
            var watchTotals = new double[Watched.Length];
            int framesWithGc = 0;

            for (int f = first; f <= last; f++)
            {
                using (var raw = ProfilerDriver.GetRawFrameDataView(f, 0))
                {
                    if (raw == null || !raw.valid) continue;
                    float playerLoop = 0f, wait = 0f; long gcBytes = 0; int gcCount = 0;
                    var watch = new double[Watched.Length];
                    int gcId = raw.GetMarkerId("GC.Alloc");
                    for (int i = 0; i < raw.sampleCount; i++)
                    {
                        var name = raw.GetSampleName(i);
                        float ms = raw.GetSampleTimeMs(i);
                        if (name == "PlayerLoop") playerLoop += ms;
                        else if (name == "WaitForTargetFPS") wait += ms;
                        else if (raw.GetSampleMarkerId(i) == gcId)
                        {
                            gcCount++;
                            if (raw.GetSampleMetadataCount(i) > 0) gcBytes += raw.GetSampleMetadataAsLong(i, 0);
                        }
                        for (int w = 0; w < Watched.Length; w++)
                            if (name.Contains(Watched[w]) && (name.EndsWith("[Invoke]") || name.EndsWith("()")))
                                watch[w] += ms;
                    }
                    float fm = raw.frameTimeMs, mw = Mathf.Max(0f, playerLoop - wait);
                    long total = Counter(raw, "Total Used Memory"), gcUsed = Counter(raw, "GC Used Memory");
                    long batches = Counter(raw, "Batches Count"), setPass = Counter(raw, "SetPass Calls Count"), tris = Counter(raw, "Triangles Count");
                    frameMs.Add(fm); work.Add(mw); gc.Add(gcBytes);
                    if (gcBytes > 0) framesWithGc++;
                    for (int w = 0; w < Watched.Length; w++) watchTotals[w] += watch[w];
                    csv.Append($"{f},{fm:0.00},{mw:0.00},{wait:0.00},{gcBytes},{gcCount},{total / 1048576.0:0.0},{gcUsed / 1048576.0:0.0},{batches},{setPass},{tris},")
                       .Append(string.Join(",", watch.Select(x => x.ToString("0.000")))).Append('\n');
                }
            }
            File.WriteAllText($"{LogDir}/frames-{stamp}.csv", csv.ToString());

            int n = frameMs.Count;
            if (n == 0) { File.WriteAllText($"{LogDir}/summary-{stamp}.txt", "No readable frames.\n"); return; }
            var sorted = frameMs.OrderBy(x => x).ToList(); var ws = work.OrderBy(x => x).ToList();
            summary.AppendLine($"Frames {first}-{last} ({n} read)");
            summary.AppendLine($"Frame time ms: avg {frameMs.Average():0.00}, median {sorted[n / 2]:0.00}, p95 {sorted[(int)(n * 0.95)]:0.00}, p99 {sorted[(int)(n * 0.99)]:0.00}, max {sorted[n - 1]:0.00}");
            summary.AppendLine($"Avg FPS {1000f / frameMs.Average():0.0}; frames over 20 ms: {frameMs.Count(x => x > 20f)} ({100f * frameMs.Count(x => x > 20f) / n:0.0}%); over 33 ms: {frameMs.Count(x => x > 33.4f)}");
            summary.AppendLine($"Main-thread work ms (PlayerLoop minus WaitForTargetFPS): avg {work.Average():0.00}, p95 {ws[(int)(n * 0.95)]:0.00}, max {ws[n - 1]:0.00}");
            summary.AppendLine($"GC alloc: frames with any {framesWithGc} ({100f * framesWithGc / n:0.0}%), avg {gc.Average():0} B/frame, max {gc.Max()} B");
            for (int w = 0; w < Watched.Length; w++)
                summary.AppendLine($"  {Watched[w]}: avg {watchTotals[w] / n:0.000} ms/frame");
            File.WriteAllText($"{LogDir}/summary-{stamp}.txt", summary.ToString());
            Debug.Log("[FireReady] Profiler stats exported:\n" + summary);
        }

        [MenuItem("Tools/FireReady/Export GC Allocators")]
        static void ExportAllocators()
        {
            Directory.CreateDirectory(LogDir);
            int last = ProfilerDriver.lastFrameIndex, first = Mathf.Max(ProfilerDriver.firstFrameIndex, last - 799);
            var totals = new Dictionary<string, double>();
            var children = new List<int>();
            int frames = 0;
            for (int f = first; f <= last; f++)
            {
                using (var view = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                           HierarchyFrameDataView.columnGcMemory, false))
                {
                    if (view == null || !view.valid) continue;
                    frames++;
                    Walk(view, view.GetRootItemID(), "", totals, children);
                }
            }
            var sb = new StringBuilder($"GC allocation by caller, frames {first}-{last} ({frames} frames)\n");
            foreach (var kv in totals.OrderByDescending(k => k.Value).Take(25))
                sb.AppendLine($"{kv.Value / Math.Max(1, frames),9:0} B/frame  {kv.Key}");
            File.WriteAllText($"{LogDir}/gc-{DateTime.Now:yyyyMMdd-HHmmss}.txt", sb.ToString());
        }

        static void Walk(HierarchyFrameDataView view, int id, string path, Dictionary<string, double> totals, List<int> scratch)
        {
            var kids = new List<int>();
            view.GetItemChildren(id, kids);
            foreach (var k in kids)
            {
                var name = view.GetItemName(k);
                if (name == "GC.Alloc")
                {
                    double bytes = view.GetItemColumnDataAsDouble(k, HierarchyFrameDataView.columnGcMemory);
                    totals.TryGetValue(path, out var t);
                    totals[path] = t + bytes;
                }
                else
                {
                    Walk(view, k, (path.Length == 0 ? name : TrimPath(path) + "/" + name), totals, scratch);
                }
            }
        }

        static string TrimPath(string path)
        {
            var parts = path.Split('/');
            return parts.Length <= 3 ? path : string.Join("/", parts.Skip(parts.Length - 3));
        }

        static long Counter(RawFrameDataView raw, string name)
        {
            int id = raw.GetMarkerId(name);
            return id == FrameDataView.invalidMarkerId ? -1 : raw.GetCounterValueAsLong(id);
        }
    }
}
