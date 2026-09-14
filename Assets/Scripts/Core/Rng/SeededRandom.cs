using System;

namespace CodexOfEchoes.Core.Rng
{
    /// <summary>
    /// xorshift32. Chosen over System.Random because System.Random's internal
    /// algorithm is not contractually stable across .NET versions, and a stored
    /// seed has to reproduce the same battle indefinitely.
    /// </summary>
    public sealed class SeededRandom : IRandomSource
    {
        private uint _state;

        public SeededRandom(int seed)
        {
            Seed = seed;
            // A zero state would make xorshift emit zeros forever.
            _state = seed == 0 ? 0x9E3779B9u : unchecked((uint)seed);
        }

        public int Seed { get; }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive), "maxExclusive must be positive.");
            }

            return (int)(NextUInt() % (uint)maxExclusive);
        }

        public int NextInclusive(int min, int max)
        {
            if (max < min)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(max), "max must be greater than or equal to min.");
            }

            return min + Next(max - min + 1);
        }

        private uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }
    }
}
