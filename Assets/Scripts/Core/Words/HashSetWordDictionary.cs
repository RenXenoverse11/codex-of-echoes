using System;
using System.Collections.Generic;

namespace CodexOfEchoes.Core.Words
{
    /// <summary>
    /// In-memory word set. ~173k ENABLE words cost roughly 10 MB here, which is fine
    /// on PC and borderline on mobile; a DAWG is the designated optimization if that
    /// becomes a constraint.
    /// </summary>
    public sealed class HashSetWordDictionary : IWordDictionary
    {
        private readonly HashSet<string> _words =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public HashSetWordDictionary(IEnumerable<string> words)
        {
            if (words == null)
            {
                throw new ArgumentNullException(nameof(words));
            }

            foreach (var word in words)
            {
                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                _words.Add(word.Trim());
            }
        }

        public int Count => _words.Count;

        public bool Contains(string word) =>
            !string.IsNullOrWhiteSpace(word) && _words.Contains(word.Trim());
    }
}
