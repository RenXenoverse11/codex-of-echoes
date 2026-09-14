using System;

namespace CodexOfEchoes.Core.Words
{
    public enum WordRejectionReason
    {
        None = 0,
        TooShort = 1,
        NotInDictionary = 2,
    }

    /// <summary>
    /// Rejections are gameplay, not errors — the engine reports a reason and consumes
    /// no turn, so the view can give distinct feedback for each case.
    /// </summary>
    public sealed class WordValidator
    {
        public const int MinimumLength = 3;

        private readonly IWordDictionary _dictionary;

        public WordValidator(IWordDictionary dictionary)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        public WordRejectionReason Validate(string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Trim().Length < MinimumLength)
            {
                return WordRejectionReason.TooShort;
            }

            return _dictionary.Contains(word)
                ? WordRejectionReason.None
                : WordRejectionReason.NotInDictionary;
        }
    }
}
