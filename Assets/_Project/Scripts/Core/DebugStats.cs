using System.Collections.Generic;

namespace FireReady.Core
{
    /// <summary>
    /// Named values for the development overlay (pool sizes, counts). Writers should
    /// update at most a few times a second, since each update builds a string.
    /// </summary>
    public static class DebugStats
    {
        static readonly SortedDictionary<string, string> s_Values = new SortedDictionary<string, string>();

        public static IEnumerable<KeyValuePair<string, string>> All => s_Values;

        public static void Set(string key, string value) => s_Values[key] = value;
    }
}
