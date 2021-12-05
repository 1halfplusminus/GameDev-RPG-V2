using Unity.Animation;
using Unity.Entities;
using Unity.Mathematics;
namespace RPG.Combat
{
    public struct EquipedBy : IComponentData
    {
        public Entity Entity;
    }
    public struct SpawnWeapon : IComponentData
    {
        public BlobAssetReference<Clip> Animation;
        public Entity Prefab;
    }
    public struct WeaponPrefab : IComponentData
    {
        public Entity Value;
    }
    public struct EquipWeapon : IComponentData
    {
        public Entity Socket;
    }
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

        public float Range;

        public bool TargetInRange;

        public int TargetFoundThisFrame;

        public bool Attacking;

        public float Cooldown;

        public float AttackDuration;

        public Attack CurrentAttack;

        public float Damage;
    }

}