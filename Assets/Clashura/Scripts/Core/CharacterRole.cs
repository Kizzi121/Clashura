using System;
using UnityEngine;

namespace Clashura.Core
{
    /// <summary>
    /// Role archetypes for playable characters in Clashura.
    /// </summary>
    public enum CharacterRole
    {
        Vanguard,
        Striker,
        Duelist,
        Specialist,
        Sentinel,
        Support
    }

    /// <summary>
    /// Base stat attributes for a character.
    /// </summary>
    [Serializable]
    public struct CharacterStats
    {
        [Range(100, 2000)]
        [SerializeField] private int maxHealth;

        [Range(10, 300)]
        [SerializeField] private int attack;

        [Range(10, 300)]
        [SerializeField] private int defense;

        [Range(1, 100)]
        [SerializeField] private int speed;

        public int MaxHealth => maxHealth;
        public int Attack => attack;
        public int Defense => defense;
        public int Speed => speed;

        public CharacterStats(int health, int attack, int defense, int speed)
        {
            this.maxHealth = Mathf.Max(1, health);
            this.attack = Mathf.Max(1, attack);
            this.defense = Mathf.Max(1, defense);
            this.speed = Mathf.Max(1, speed);
        }
    }
}
