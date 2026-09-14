using System;

namespace CodexOfEchoes.Core.Combat
{
    /// <summary>A fighter's health. Plain and serialization-friendly for cross-save.</summary>
    public sealed class Combatant
    {
        public Combatant(string name, int maxHp)
        {
            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), "maxHp must be positive.");
            }

            Name = name;
            MaxHp = maxHp;
            Hp = maxHp;
        }

        public string Name { get; }

        public int MaxHp { get; }

        public int Hp { get; private set; }

        public bool IsDefeated => Hp <= 0;

        public int TakeDamage(int amount)
        {
            if (amount > 0)
            {
                Hp = Math.Max(0, Hp - amount);
            }

            return Hp;
        }

        public int Heal(int amount)
        {
            if (amount > 0)
            {
                Hp = Math.Min(MaxHp, Hp + amount);
            }

            return Hp;
        }
    }
}
