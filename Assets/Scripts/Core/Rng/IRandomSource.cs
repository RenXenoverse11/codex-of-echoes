namespace CodexOfEchoes.Core.Rng
{
    /// <summary>
    /// All randomness in the battle engine flows through this, so any battle can be
    /// replayed exactly from its seed.
    /// </summary>
    public interface IRandomSource
    {
        int Seed { get; }

        /// <summary>Returns a value in [0, maxExclusive).</summary>
        int Next(int maxExclusive);

        /// <summary>Returns a value in [min, max], both ends reachable.</summary>
        int NextInclusive(int min, int max);
    }
}
