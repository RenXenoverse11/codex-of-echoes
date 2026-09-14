using System;
using System.Collections.Generic;
using System.Linq;

namespace CodexOfEchoes.Core.Grid
{
    /// <summary>
    /// Letter point values and draw weights. Values are Scrabble-like but compressed;
    /// weights are vowel-boosted relative to natural English frequency (42/98 vowels)
    /// because a word game with realistic frequencies produces unplayable boards.
    /// </summary>
    public static class LetterTable
    {
        private static readonly Dictionary<string, int> Values =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "E", 1 }, { "A", 1 }, { "I", 1 }, { "O", 1 }, { "N", 1 },
                { "R", 1 }, { "T", 1 }, { "L", 1 }, { "S", 1 }, { "U", 1 },
                { "D", 2 }, { "G", 2 },
                { "B", 3 }, { "C", 3 }, { "M", 3 }, { "P", 3 },
                { "F", 4 }, { "H", 4 }, { "V", 4 }, { "W", 4 }, { "Y", 4 },
                { "K", 5 },
                { "J", 8 }, { "X", 8 },
                { "Z", 10 }, { "Qu", 10 },
            };

        private static readonly Dictionary<string, int> Weights =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "E", 12 },
                { "A", 9 }, { "I", 9 },
                { "O", 8 },
                { "N", 6 }, { "R", 6 }, { "T", 6 },
                { "L", 4 }, { "S", 4 }, { "U", 4 }, { "D", 4 },
                { "G", 3 },
                { "B", 2 }, { "C", 2 }, { "M", 2 }, { "P", 2 }, { "F", 2 },
                { "H", 2 }, { "V", 2 }, { "W", 2 }, { "Y", 2 },
                { "K", 1 }, { "J", 1 }, { "X", 1 }, { "Z", 1 }, { "Qu", 1 },
            };

        private static readonly HashSet<string> Vowels =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A", "E", "I", "O", "U" };

        public static IReadOnlyList<string> AllLetters { get; } = Values.Keys.ToList();

        public static int TotalWeight { get; } = Weights.Values.Sum();

        public static int ValueOf(string letter) => Values[letter];

        public static int WeightOf(string letter) => Weights[letter];

        public static bool IsVowel(string letter) => Vowels.Contains(letter);

        public static LetterTile CreateTile(string letter) =>
            new LetterTile(Normalize(letter), ValueOf(letter));

        /// <summary>Maps any casing onto the canonical spelling used on tiles.</summary>
        private static string Normalize(string letter)
        {
            foreach (var canonical in Values.Keys)
            {
                if (string.Equals(canonical, letter, StringComparison.OrdinalIgnoreCase))
                {
                    return canonical;
                }
            }

            throw new ArgumentException($"Unknown letter '{letter}'.", nameof(letter));
        }
    }
}
