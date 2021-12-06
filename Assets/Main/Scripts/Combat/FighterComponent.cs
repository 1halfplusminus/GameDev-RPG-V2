using Unity.Animation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
namespace RPG.Combat
{

    public struct PickableWeapon : IComponentData
    {
        public Entity Entity;
    }

    public struct Picked : IComponentData
    {

    }
    public struct RightHandWeaponSocket : IComponentData
    {
        public Entity Entity;
    }
    public struct LeftHandWeaponSocket : IComponentData
    {
        public Entity Entity;
    }

    public struct Equipped : IComponentData
    {
        public Entity Entity;
    }
    public struct EquipedBy : IComponentData
    {
        public Entity Entity;
    }
    public struct SpawnWeapon : IComponentData
    {
        public BlobAssetReference<Clip> Animation;
        public Entity Prefab;
        public Entity Weapon;
    }
    public struct EquippedPrefab : IComponentData
    {
        public Entity Value;
    }
    public struct FighterEquip : IComponentData
    {
        public Entity Entity;
    }
    public struct EquipInSocket : IComponentData
    {
        public Entity Socket;
    }
    public struct Weapon : IComponentData
    {
        public float Damage;
        public float Range;
        public float Cooldown;
        public float AttackDuration;
        public FixedList32<float> HitEvents;
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