namespace CodexOfEchoes.Core.Grid
{
    /// <summary>
    /// One tile on the board. Letter is a string rather than a char because "Qu"
    /// occupies a single tile.
    /// </summary>
    public readonly struct LetterTile
    {
        public LetterTile(string letter, int value)
        {
            Letter = letter;
            Value = value;
        }

        public string Letter { get; }

        public int Value { get; }

        public override string ToString() => $"{Letter}({Value})";
    }
}
