

using Unity.Entities;

namespace RPG.Core
{
    public enum CharacterStateMask : int
    {
        None = 0,
        Alive = 1 << 0,
        Attacking = 1 << 1,
        Moving = 1 << 2,
        Dead = 1 << 6
    }
    public struct IsDeadTag : IComponentData { }
    public struct CharacterState : IComponentData
    {
        public CharacterStateMask State;

    }
}