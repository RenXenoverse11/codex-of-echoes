using System;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Words;

namespace CodexOfEchoes.Core.Battle
{
    /// <summary>
    /// Everything the engine needs to start a battle. The Unity layer builds this from
    /// ScriptableObjects; tests build it inline.
    /// </summary>
    public sealed class BattleSetup
    {
        public BattleSetup(
            IWordDictionary dictionary,
            StoryWordTable storyWords,
            string enemyName,
            int enemyMaxHp,
            int playerMaxHp,
            int seed,
            EnemyKind enemyKind = EnemyKind.Aswang)
        {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            StoryWords = storyWords ?? throw new ArgumentNullException(nameof(storyWords));
            EnemyName = enemyName;
            EnemyMaxHp = enemyMaxHp;
            PlayerMaxHp = playerMaxHp;
            Seed = seed;
            EnemyKind = enemyKind;
        }

        public IWordDictionary Dictionary { get; }

        public StoryWordTable StoryWords { get; }

        public string EnemyName { get; }

        public int EnemyMaxHp { get; }

        public int PlayerMaxHp { get; }

        public int Seed { get; }

        public EnemyKind EnemyKind { get; }
    }
}
