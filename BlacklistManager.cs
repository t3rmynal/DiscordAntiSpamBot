using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AntiSpamBot
{
    public static class BlacklistManager
    {
        private static readonly string FilePath = "blacklist.txt";
        private static readonly HashSet<string> Blacklist = new(StringComparer.OrdinalIgnoreCase);

        public static async Task InitializeAsync()
        {
            if (!File.Exists(FilePath))
            {
                await File.WriteAllTextAsync(FilePath, string.Empty);
                return;
            }

            var lines = await File.ReadAllLinesAsync(FilePath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    Blacklist.Add(trimmed);
            }
        }

        public static bool Contains(string text)
        {
            foreach (var bad in Blacklist)
            {
                if (text.Contains(bad, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static async Task<bool> AddWordAsync(string word)
        {
            word = word.Trim().ToLowerInvariant();

            if (Blacklist.Contains(word))
                return false;

            Blacklist.Add(word);
            await File.AppendAllLinesAsync(FilePath, new[] { word });
            return true;
        }

        public static IEnumerable<string> GetAll() => Blacklist;
    }
}
