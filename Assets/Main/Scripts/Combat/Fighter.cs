using RPG.Mouvement;
using Unity.Entities;

namespace RPG.Combat
{

    public struct Hit : IComponentData
    {
        public Entity Hitter;
        public Entity Hitted;

        public float Damage;
    }
    public struct HitEvent : IBufferElementData
    {
        public float Time;
        public bool Fired;
    }

    public struct Attack : IComponentData
    {
        public float Cooldown;

        public float TimeElapsedSinceAttack;

        public bool InCooldown;


    }



    public struct Fighter : IComponentData
    {
        public Entity Target;

        public bool MoveTowardTarget;

        public float WeaponRange;

        public bool TargetInRange;

        public int TargetFoundThisFrame;

        public bool Attacking;

        public float AttackCooldown;

        public float AttackDuration;

        public Attack currentAttack;

        public float WeaponDamage;
    }

}