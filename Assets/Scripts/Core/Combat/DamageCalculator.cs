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

            var lengthMultiplier = 1.0f + (LengthStep * Math.Max(0, wordLength - LengthFloor));
            var raw = baseDamage * lengthMultiplier * StoryWordTable.MultiplierFor(tier);

            // Half-away-from-zero, not C#'s default banker's rounding, which would
            // shave damage off roughly half of all midpoint results.
            return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
        }
    }
}
