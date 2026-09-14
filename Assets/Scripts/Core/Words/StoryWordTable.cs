using System;
using System.Collections.Generic;

namespace CodexOfEchoes.Core.Words
{
    public enum StoryWordTier
    {
        None = 0,
        Echo = 1,
        Bane = 2,
    }

    /// <summary>
    /// The mechanic that separates this from a Bookworm reimplementation: a player who
    /// reads the myth fights better. Echo words are chapter-wide; Bane words are the
    /// specific folklore weaknesses of one enemy.
    /// </summary>
    public sealed class StoryWordTable
    {
        private readonly HashSet<string> _echo =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _bane =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public StoryWordTable(IEnumerable<string> echoWords, IEnumerable<string> baneWords)
        {
            AddAll(_echo, echoWords);
            AddAll(_bane, baneWords);
        }

        public static float MultiplierFor(StoryWordTier tier)
        {
            switch (tier)
            {
                case StoryWordTier.Bane: return 2.5f;
                case StoryWordTier.Echo: return 1.5f;
                default: return 1.0f;
            }
        }

        /// <summary>Bane takes precedence when a word appears in both sets.</summary>
        public StoryWordTier TierOf(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return StoryWordTier.None;
            }

            var trimmed = word.Trim();

            if (_bane.Contains(trimmed))
            {
                return StoryWordTier.Bane;
            }

            return _echo.Contains(trimmed) ? StoryWordTier.Echo : StoryWordTier.None;
        }

        private static void AddAll(HashSet<string> target, IEnumerable<string> words)
        {
            if (words == null)
            {
                return;
            }

            foreach (var word in words)
            {
                if (!string.IsNullOrWhiteSpace(word))
                {
                    target.Add(word.Trim());
                }
            }
        }
    }
}
