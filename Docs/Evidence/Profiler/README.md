# Profiler evidence (iPhone XR, development build)

Captured 21 Oct 2026 with the Unity Profiler connected to a development build on an iPhone XR.
The Profiler keeps the last 2,000 frames, so this run covers about 33 seconds:
menu and scenario select, loading, scanning and placement, the briefing, then about 13 seconds
of continuous spraying with sweep in scenario S2 (kitchen pan).

Numbers come from `Tools > FireReady > Export Profiler Stats` and `Export GC Allocators`
(`Assets/_Project/Editor/Diagnostics/ProfilerStatsExporter.cs`), which read every frame
through `ProfilerDriver` and write `frames.csv`, `summary.txt` and `gc_allocators.txt`.

| Measure | Whole run (1,997 frames) | Spraying only (796 frames) |
|---|---|---|
| Average frame time | 16.66 ms (60.0 fps) | 16.66 ms (60.0 fps) |
| 95th / 99th percentile | 18.6 / 19.5 ms | 19.0 / 19.7 ms |
| Worst frame | 21.5 ms (scene placement) | 21.3 ms |
| Frames over 20 ms | 5 (0.3%) | 4 (0.5%) |
| Frames over 33 ms | 0 | 0 |
| Main-thread work, average | 4.5 ms | 4.5 ms |
| Main-thread work, 95th percentile | 7.4 ms | 6.2 ms |
| Total used memory | 241 to 254 MB | 248 to 250 MB |
| SetPass calls | 6 to 28 | 20 to 28 |

The frame rate is held by vsync at 60 fps. The main thread does about 4.5 ms of work per
frame, roughly a quarter of the 16.7 ms budget; the rest is spent waiting for the next frame.

**Garbage collection.** About 5.9 KB is allocated per frame while spraying. `gc_allocators.txt`
shows that about 95% of it comes from `ARDebugOverlay.OnGUI` (the on-screen development
readout), which is compiled only under `DEVELOPMENT_BUILD || UNITY_EDITOR` and is absent
from the release build. Gameplay scripts allocate under 30 bytes per frame on average
(ExtinguisherController about 25 B, ScenarioController about 16 B), from occasional event
strings, not from per-frame work. Spray impacts come from a prewarmed `ObjectPool`, so
spraying creates no new GameObjects.

The full capture (`capture-*.data`, about 100 MB) is kept locally in `Logs/Profiler/` and
can be opened with the Profiler's Load button. It is not committed because of its size.
