using System;
using System.IO;
using CodexOfEchoes.Core.Words;
using UnityEngine;

namespace CodexOfEchoes.Game.Data
{
    /// <summary>
    /// Loads the ENABLE word list into Core. A missing or empty file is a developer
    /// error and fails loudly at load — a clear startup failure beats a battle that
    /// limps with an empty dictionary.
    /// </summary>
    public static class DictionaryLoader
    {
        public const string DefaultFileName = "enable.txt";

        public static IWordDictionary LoadFromStreamingAssets(
            string fileName = DefaultFileName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, fileName);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Word list not found at '{path}'. Place the ENABLE word list there. "
                    + "Do not substitute TWL or SOWPODS: both are licensed and unsafe to "
                    + "ship commercially.",
                    path);
            }

            var lines = File.ReadAllLines(path);
            var dictionary = new HashSetWordDictionary(lines);

            if (dictionary.Count == 0)
            {
                throw new InvalidDataException($"Word list at '{path}' contained no words.");
            }

            Debug.Log($"[Codex] Loaded {dictionary.Count:N0} words from {fileName}.");
            return dictionary;
        }
    }
}
