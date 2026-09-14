using System;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Words;

namespace CodexOfEchoes.Game.Data
{
    /// <summary>
    /// The single place Unity assets become plain Core values. Keeping this conversion
    /// in one method is what lets Core stay serialization-friendly for cross-save.
    /// </summary>
    public static class BattleSetupFactory
    {
        public static BattleSetup Create(
            IWordDictionary dictionary,
            EnemyDefinitionSO enemy,
            StoryWordSetSO storyWords,
            BalanceConfigSO balance,
            int seed)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            if (storyWords == null) throw new ArgumentNullException(nameof(storyWords));
            if (balance == null) throw new ArgumentNullException(nameof(balance));

            return new BattleSetup(
                dictionary: dictionary,
                storyWords: new StoryWordTable(storyWords.EchoWords, enemy.BaneWords),
                enemyName: enemy.EnemyName,
                enemyMaxHp: enemy.MaxHp,
                playerMaxHp: balance.PlayerMaxHp,
                seed: seed);
        }
    }
}
