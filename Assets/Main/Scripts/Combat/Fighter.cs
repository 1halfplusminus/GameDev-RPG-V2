using Unity.Entities;
using Unity.Mathematics;
namespace RPG.Combat
{
    public struct IsFighting : IComponentData { }
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


        public static Attack Create()
        {

            return new Attack() { Cooldown = math.EPSILON, TimeElapsedSinceAttack = math.INFINITY, InCooldown = false };
        }
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

        public Attack CurrentAttack;

        public float WeaponDamage;
    }

}