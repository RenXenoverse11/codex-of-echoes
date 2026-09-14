using System;
using System.Collections.Generic;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;

namespace CodexOfEchoes.Core.Combat
{
    /// <summary>
    /// damage = round(base x lengthMultiplier x storyMultiplier)
    ///
    /// base sums per-tile point values; wordLength counts CHARACTERS, not tiles, so a
    /// "Qu" tile contributes 2 to length. The spec defines Qu as one tile but does not
    /// say which measure the formula uses; characters match what the player sees, and
    /// Qu's value of 10 already rewards its rarity.
    /// </summary>
    public static class DamageCalculator
    {
        public const float LengthStep = 0.22f;
        private const int LengthFloor = 3;

        public static int Compute(
            IReadOnlyList<LetterTile> tiles, int wordLength, StoryWordTier tier)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return 0;
            }

            var baseDamage = 0;
            foreach (var tile in tiles)
            {
                baseDamage += tile.Value;
            }

            var k = Math.Max(0, wordLength - LengthFloor);
            var story10 = StoryMultiplierTimesTen(tier);

            // damage = base x (1 + 0.22k) x story, computed exactly (scaled by 1000, no
            // floating point anywhere) then rounded half-away-from-zero. Float arithmetic
            // here previously made true midpoints (e.g. 31.5) arrive as values like
            // 31.499998 due to 0.22f's binary imprecision, silently rounding the wrong
            // way for ~26% of real midpoint words.
            long numerator = (long)baseDamage * (100 + (22 * k)) * story10;
            return (int)((numerator * 2 + 1000) / 2000);
        }

        private static int StoryMultiplierTimesTen(StoryWordTier tier)
        {
            switch (tier)
            {
                case StoryWordTier.Bane: return 25;
                case StoryWordTier.Echo: return 15;
                default: return 10;
            }
        }
    }
}
