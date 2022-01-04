
using System;

namespace RPG.Stats
{
    [Serializable]
    public enum CharacterClass : byte
    {
        Novice = 0x1,
        Mage = 0x2,
        Archer = 0x3,
        Warrior = 0x4,
        Soldier = 0x5,
        HeavySoldier = 0x6,
        Minion = 0x7,
        Thug = 0x8,
        Knight = 0x9
    }
}