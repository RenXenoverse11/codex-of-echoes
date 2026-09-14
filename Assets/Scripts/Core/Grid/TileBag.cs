using System;
using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Rng;

namespace CodexOfEchoes.Core.Grid
{
    /// <summary>
    /// Weighted infinite draw. Not a finite bag — tiles are never exhausted, so the
    /// board can always refill.
    /// </summary>
    public sealed class TileBag
    {
        private readonly IRandomSource _rng;
        private readonly List<string> _letters;
        private readonly List<int> _cumulativeWeights;
        private readonly List<string> _vowels;

        public TileBag(IRandomSource rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));

            _letters = LetterTable.AllLetters.ToList();
            _vowels = _letters.Where(LetterTable.IsVowel).ToList();

            _cumulativeWeights = new List<int>(_letters.Count);
            var running = 0;
            foreach (var letter in _letters)
            {
                running += LetterTable.WeightOf(letter);
                _cumulativeWeights.Add(running);
            }
        }

        public LetterTile Draw() => DrawFrom(_letters, _cumulativeWeights, LetterTable.TotalWeight);

        public LetterTile DrawVowel()
        {
            var vowelWeights = new List<int>(_vowels.Count);
            var running = 0;
            foreach (var vowel in _vowels)
            {
                running += LetterTable.WeightOf(vowel);
                vowelWeights.Add(running);
            }

            return DrawFrom(_vowels, vowelWeights, running);
        }

        private LetterTile DrawFrom(
            IReadOnlyList<string> letters, IReadOnlyList<int> cumulative, int total)
        {
            var roll = _rng.Next(total);

            for (var i = 0; i < cumulative.Count; i++)
            {
                if (roll < cumulative[i])
                {
                    return LetterTable.CreateTile(letters[i]);
                }
            }

            return LetterTable.CreateTile(letters[letters.Count - 1]);
        }
    }
}
