using Unity.Animation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;

namespace RPG.Combat
{
    public struct WeaponBlobAsset
    {
        public Weapon Weapon;

        public BlobArray<float> HitEvents;

        public Hash128 GUID;
        public Entity Entity;
        public Entity Prefab;
        public Entity ProjectileEntity;
    }
    public struct ProjectileHitted : IComponentData { }

    public struct IsHomingProjectile : IComponentData
    {

    }
    public struct IsProjectile : IComponentData { }
    public struct NoDamage : IComponentData { }
    public struct TargetLooked : IComponentData
    {

    }
    public struct TargetLook : IComponentData
    {
        public float3 TargetDirection;

    }

    public struct ShootProjectile : IComponentData
    {
        public Entity Prefab;
        public float Speed;

        public Entity Socket;

    }
    public struct Projectile : IComponentData
    {
        public Entity Target;
        public float Speed;
        public bool IsHoming;
        public Entity ShootBy;
        public int MaxTargetHit;
    }
    public struct EquipableSockets : IComponentData
    {
        public Entity LeftHandSocket;
        public Entity RightHandSocket;

        public Entity GetSocketForWeapon(Weapon weapon)
        {
            return GetSocketForType(weapon.SocketType);
        }
        public Entity GetSocketForType(SocketType type)
        {
            return type switch
            {
                SocketType.LeftHand => LeftHandSocket,
                SocketType.RightHand => RightHandSocket,
                _ => Entity.Null,
            };
        }
        public FixedList32Bytes<Entity> ToList()
        {
            var fixedlist = new FixedList32Bytes<Entity>
            {
                LeftHandSocket,
                RightHandSocket
            };
            return fixedlist;
        }
        public IEnumerable<Entity> All()
        {
            yield return LeftHandSocket;
            yield return RightHandSocket;
        }

        public IEnumerable<Entity> GetEnumerator()
        {
            return All();
        }
    }
    public struct PickableWeapon : IComponentData
    {
        public Entity Entity;
        public SocketType SocketType;
    }

    public struct Picked : IComponentData
    {

    }

    public struct LeftHandWeaponSocket : IComponentData
    {
        public Entity Entity;
    }

    public struct FighterEquipped : IComponentData
    {

        public BlobAssetReference<WeaponBlobAsset> WeaponAsset;

        public Entity Instance;

    }
    public struct EquippedBy : IComponentData
    {
        public Entity Entity;
    }
    public struct UnEquiped : IComponentData
    {

    }
    public struct Equipped : IComponentData
    {
        public BlobAssetReference<WeaponBlobAsset> Equipable;
    }
    public struct SpawnWeapon : IComponentData
    {
        public BlobAssetReference<Clip> Animation;
        public Entity Prefab;
        public BlobAssetReference<WeaponBlobAsset> Weapon;
        public Entity Projectile;
    }
    public struct EquippedPrefab : IComponentData
    {
        public Entity Value;
    }
    public struct Equip : IComponentData
    {
        public Entity Equipable;

        public SocketType SocketType;
    }

    public struct RemoveEquipedInSocket : IComponentData
    {

    }
    public struct EquipInSocket : IComponentData
    {
        public Entity Socket;

        public Entity Weapon;



    }

    public enum SocketType
    {
        LeftHand, RightHand
    }
    public struct Weapon : IComponentData
    {
        public float Damage;
        public float Range;
        public float Cooldown;
        public float BonusDamagePercent;
        public float AttackDuration;
        public FixedString64Bytes GUID;
        public FixedList32Bytes<float> HitEvents;

        public SocketType SocketType;
    }
    public struct ProjectileShooted : IComponentData { }
    public struct IsFighting : IComponentData { }
    public struct HasIt : IBufferElementData
    {
        public Entity Hit;
    }

    public struct WasHitted : IComponentData
    {

    }
    public struct Hitter : IComponentData
    {
        public Entity Value;
    }
    public struct WasHitteds : IBufferElementData
    {
        public Entity Hitter;
    }
    public struct Hit : IComponentData
    {
        public Entity Hitter;
        public Entity Hitted;

        public Entity Trigger;

        public float Damage;
    }
    public struct HitEvent : IBufferElementData
    {
        public float Time;
        public bool Fired;
        public Entity Trigger;
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