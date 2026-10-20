using System;
using System.Collections.Generic;
using System.IO;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Data
{
    /// <summary>
    /// Saves sessions as JSON under Application.persistentDataPath/sessions.
    /// Every write goes to a temporary file first and then replaces the real one,
    /// so a crash mid-write never leaves a half-written file. If index.json is
    /// missing or damaged it is rebuilt from the session files.
    /// </summary>
    public sealed class JsonSessionRepository : ISessionHistory
    {
        const string IndexFile = "index.json";
        const int MaxSessions = 50;

        readonly string m_Folder;
        SessionIndex m_Index;

        public JsonSessionRepository(string rootFolder)
        {
            m_Folder = Path.Combine(rootFolder, "sessions");
            Directory.CreateDirectory(m_Folder);
            m_Index = LoadIndex();
        }

        public string Folder => m_Folder;
        public int Count => m_Index.sessions.Count;

        public IReadOnlyList<SessionSummary> Recent(int max)
        {
            if (max >= m_Index.sessions.Count)
                return m_Index.sessions;
            return m_Index.sessions.GetRange(0, max);
        }

        /// <summary>Best score so far for a scenario, or -1 if it has never been played.</summary>
        public int BestScore(string title)
        {
            int best = -1;
            foreach (var s in m_Index.sessions)
                if (s.title == title && s.score > best)
                    best = s.score;
            return best;
        }

        public void Save(SessionRecord record)
        {
            if (string.IsNullOrEmpty(record.id))
                record.id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);

            WriteAtomic(PathFor(record.id), JsonUtility.ToJson(record, true));

            m_Index.sessions.RemoveAll(s => s.id == record.id);
            m_Index.sessions.Insert(0, record.ToSummary());

            // Keep storage bounded: drop the oldest sessions beyond the cap.
            while (m_Index.sessions.Count > MaxSessions)
            {
                var oldest = m_Index.sessions[m_Index.sessions.Count - 1];
                m_Index.sessions.RemoveAt(m_Index.sessions.Count - 1);
                TryDelete(PathFor(oldest.id));
            }

            SaveIndex();
        }

        public SessionRecord Load(string id)
        {
            var path = PathFor(id);
            if (!File.Exists(path))
                return null;
            try { return JsonUtility.FromJson<SessionRecord>(File.ReadAllText(path)); }
            catch (Exception e)
            {
                Debug.LogWarning($"[Sessions] Could not read {id}: {e.Message}");
                return null;
            }
        }

        public void DeleteAll()
        {
            foreach (var file in Directory.GetFiles(m_Folder, "*.json"))
                TryDelete(file);
            m_Index = new SessionIndex();
            SaveIndex();
        }

        // ---- Files ------------------------------------------------------------------------

        string PathFor(string id) => Path.Combine(m_Folder, id + ".json");

        void SaveIndex() => WriteAtomic(Path.Combine(m_Folder, IndexFile), JsonUtility.ToJson(m_Index, true));

        SessionIndex LoadIndex()
        {
            var path = Path.Combine(m_Folder, IndexFile);
            if (File.Exists(path))
            {
                try
                {
                    var index = JsonUtility.FromJson<SessionIndex>(File.ReadAllText(path));
                    if (index != null && index.sessions != null)
                        return index;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Sessions] index.json is damaged, rebuilding: {e.Message}");
                }
            }
            return RebuildIndex();
        }

        SessionIndex RebuildIndex()
        {
            var index = new SessionIndex();
            foreach (var file in Directory.GetFiles(m_Folder, "*.json"))
            {
                if (Path.GetFileName(file) == IndexFile)
                    continue;
                try
                {
                    var record = JsonUtility.FromJson<SessionRecord>(File.ReadAllText(file));
                    if (record != null && !string.IsNullOrEmpty(record.id))
                        index.sessions.Add(record.ToSummary());
                }
                catch (Exception) { /* skip unreadable files */ }
            }
            index.sessions.Sort((a, b) => string.CompareOrdinal(b.endedUtc, a.endedUtc));
            m_Index = index;
            SaveIndex();
            return index;
        }

        static void WriteAtomic(string path, string contents)
        {
            var temp = path + ".tmp";
            File.WriteAllText(temp, contents);
            if (File.Exists(path))
            {
                try
                {
                    File.Replace(temp, path, null);
                    return;
                }
                catch (Exception)
                {
                    File.Delete(path); // platforms without File.Replace
                }
            }
            File.Move(temp, path);
        }

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception) { }
        }
    }
}
