
using Unity.Entities;
using RPG.Core;
using RPG.Animation;
using Unity.Collections;
using System;
using Unity.Animation;
using Unity.Animation.Hybrid;

namespace RPG.Combat
{


    public struct Equipable : IComponentData
    {
        public FixedString64Bytes GUID;
    }
    [Serializable]
    public struct Addressable : ISharedComponentData
    {
        public Hash128 Value;
    }
    public struct WeaponAssetData : IComponentData
    {
        public BlobAssetReference<WeaponBlobAsset> Weapon;

    }

    public class WeaponConversionSystem : GameObjectConversionSystem
    {

        protected override void OnCreate()
        {
            base.OnCreate();

        }
        public BlobAssetReference<WeaponBlobAsset> GetWeapon(WeaponAsset r)
        {

            UnityEngine.Hash128 hash = new Hash128();
            hash.Append(r.GUID);
            if (!BlobAssetStore.TryGet(hash, out BlobAssetReference<WeaponBlobAsset> weaponBlobAssetRef))
            {
                using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
                {

                    // Take note of the "ref" keywords. Unity will throw an error without them, since we're working with structs.
                    ref var weaponBlobAsset = ref blobBuilder.ConstructRoot<WeaponBlobAsset>();
                    weaponBlobAsset.Weapon = Convert(r);
                    weaponBlobAsset.GUID = hash;
                    blobBuilder.Construct(ref weaponBlobAsset.HitEvents, weaponBlobAsset.Weapon.HitEvents.ToArray());
                    // Store the created reference to the memory location of the blob asset
                    weaponBlobAssetRef = blobBuilder.CreateBlobAssetReference<WeaponBlobAsset>(Allocator.Persistent);
                    BlobAssetStore.TryAdd(hash, weaponBlobAssetRef);
                    return weaponBlobAssetRef;
                }
            }
            return weaponBlobAssetRef;
        }
        public static Weapon Convert(WeaponAsset weapon)
        {
            return new Weapon
            {
                AttackDuration = weapon.AttackDuration,
                BonusDamagePercent = weapon.BonusDamagePercent,
                Cooldown = 0.1f,
                Damage = weapon.Damage,
                Range = weapon.Range,
                SocketType = weapon.SocketType,
                HitEvents = CreateHitEvent(weapon),
                GUID = weapon.GUID
            };
        }
        protected override void OnUpdate()
        {
            Entities.ForEach((WeaponAsset weapon) =>
            {

                var weaponEntity = GetPrimaryEntity(weapon);
                var weaponPrefab = GetPrimaryEntity(weapon.WeaponPrefab);
                DstEntityManager.AddComponentData(weaponEntity, new EquippedPrefab { Value = weaponPrefab });
                DstEntityManager.AddComponentData(weaponEntity, Convert(weapon));
                DstEntityManager.AddComponentData(weaponEntity, new Equipable { GUID = weapon.GUID });
                var projectileEntity = TryGetPrimaryEntity(weapon.Projectile);
                DstEntityManager.AddComponentData(weaponEntity, new ShootProjectile() { Prefab = projectileEntity });
                BlobAssetReference<WeaponBlobAsset> weaponBlobAssetRef = GetWeapon(weapon);
                weaponBlobAssetRef.Value.Entity = weaponEntity;
                weaponBlobAssetRef.Value.ProjectileEntity = projectileEntity;
                weaponBlobAssetRef.Value.Prefab = weaponPrefab;
                var hash = new UnityEngine.Hash128();
                if (!string.IsNullOrEmpty(weapon.GUID))
                {
                    hash.Append(weapon.GUID);
                }
                hash.Append(weapon.Animation.name);
                if (!BlobAssetStore.TryGet(hash, out BlobAssetReference<Clip> blobAssetReferenceClip))
                {

                    blobAssetReferenceClip = weapon.Clip.GetClip();
                }
                if (blobAssetReferenceClip.IsCreated)
                {

                    DstEntityManager.AddComponentData(weaponEntity, new ChangeAttackAnimation { Animation = blobAssetReferenceClip });
                }
                DstEntityManager.AddComponentData(weaponEntity, new WeaponAssetData() { Weapon = weaponBlobAssetRef });
                // DstEntityManager.AddComponent<Asset>(weaponEntity);
            });
        }
        private static FixedList32Bytes<float> CreateHitEvent(WeaponAsset weapon)
        {

            var hitEvents = new FixedList32Bytes<float>
            {
                Length = weapon.HitEvents.Count
            };
            for (int i = 0; i < weapon.HitEvents.Count; i++)
            {
                hitEvents[i] = weapon.HitEvents[i];
            }
            return hitEvents;
        }
    }

    public class ProjectileConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ProjectileAuthoring p) =>
            {
                var target = GetPrimaryEntity(p.Target);
                var entity = GetPrimaryEntity(p);
                if (p.IsHoming)
                {
                    DstEntityManager.AddComponent<IsHomingProjectile>(entity);
                }
                DstEntityManager.AddComponent<StatefulTriggerEvent>(entity);
                DstEntityManager.AddComponentData(entity, new Projectile
                {
                    Target = target,
                    Speed = p.Speed,
                    IsHoming = p.IsHoming,
                    MaxTargetHit = p.MaxHitTarget
                });
                DstEntityManager.AddComponent<Spawned>(entity);
                DstEntityManager.AddComponent<DeltaTime>(entity);
            });
        }
    }
    [UpdateAfter(typeof(RigConversion))]
    public class FighterConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((FighterAuthoring fighter) =>
            {
                var entity = GetPrimaryEntity(fighter);

                DstEntityManager.AddComponent<HittedByRaycastEvent>(entity);

                //FIXME: Weapon value change be assigned when weapon is equiped
                DstEntityManager.AddComponentData(entity, new Fighter
                {
                    CurrentAttack = Attack.Create()
                });
                DstEntityManager.AddBuffer<HitEvent>(entity);
                DstEntityManager.AddComponent<LookAt>(entity);
                DstEntityManager.AddComponent<DeltaTime>(entity);
                DstEntityManager.AddBuffer<WasHitteds>(entity);
                var equipableSockets = new EquipableSockets
                {
                    LeftHandSocket = TryGetPrimaryEntity(fighter.LeftHandSocket),
                    RightHandSocket = TryGetPrimaryEntity(fighter.RightHandSocket)
                };
                DstEntityManager.AddComponentData(entity, equipableSockets);
                foreach (var socket in equipableSockets.All())
                {
                    if (socket != Entity.Null)
                    {
                        DstEntityManager.AddComponentData(socket, new EquippedBy { Entity = entity });
                    }

                }
                var weapon = fighter.Weapon;
                if (weapon != null)
                {
                    var weaponEntity = GetPrimaryEntity(weapon);
                    var socketEntity = equipableSockets.GetSocketForType(weapon.SocketType);
                    if (socketEntity != Entity.Null)
                    {
                        DstEntityManager.AddComponentData(socketEntity, new EquipInSocket { Socket = socketEntity, Weapon = weaponEntity });
                    }
                    //FIXME: Make a system that calcule hit point from physics collider if no hit point
                    var hitPointEntity = TryGetPrimaryEntity(fighter.HitPoint);
                    hitPointEntity = hitPointEntity == Entity.Null ? entity : hitPointEntity;
                    DstEntityManager.AddComponentData(hitPointEntity, new HitPoint { Entity = entity });
                }

            });
        }


    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class FighterDeclareReferencedObjectsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((FighterAuthoring fighter) =>
            {
                if (fighter.Weapon != null)
                {
                    DeclareReferencedAsset(fighter.Weapon);
                    DeclareAssetDependency(fighter.gameObject, fighter.Weapon);
                    // if (fighter.RightHandSocket.gameObject.GetComponent<LateAnimationGraphWriteTransformHandle>() == null)
                    // {
                    //     fighter.RightHandSocket.gameObject.AddComponent<LateAnimationGraphWriteTransformHandle>();
                    // }

                    // if (fighter.LeftHandSocket.gameObject.GetComponent<LateAnimationGraphWriteTransformHandle>() == null)
                    // {
                    //     fighter.LeftHandSocket.gameObject.AddComponent<LateAnimationGraphWriteTransformHandle>();
                    // }

                }

            });
        }
    }


    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class WeaponDeclareReferencedObjectsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((WeaponAsset weapon) =>
            {
                DeclareReferencedPrefab(weapon.WeaponPrefab);
                DeclareReferencedPrefab(weapon.Projectile);
            });
        }

    }
    [UpdateAfter(typeof(WeaponConversionSystem))]
    public class WeaponAuthoringConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((WeaponAuthoring weaponAuthoring) =>
            {
                var weaponAuthoringEntity = GetPrimaryEntity(weaponAuthoring);
                var weaponAssetEntity = GetPrimaryEntity(weaponAuthoring.WeaponAsset);
                var hash = new UnityEngine.Hash128();
                hash.Append(weaponAuthoring.WeaponAsset.GUID);
                // BlobAssetStore.TryGet<WeaponAssetData>(hash, out var weaponBlobAssetRef);
                DstEntityManager.AddComponentData(weaponAuthoringEntity, new WeaponAssetReference
                {
                    GUID = hash,
                    Address = weaponAuthoring.WeaponAsset.GUID,

                });

            });
        }

    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class WeaponAuthoringDeclareReferencedObjectsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((WeaponAuthoring weaponAuthoring) =>
            {
                DeclareReferencedAsset(weaponAuthoring.WeaponAsset);
                DeclareAssetDependency(weaponAuthoring.gameObject, weaponAuthoring.WeaponAsset);
            });
        }

    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class WeaponPickupDeclareReferencedObjectsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((WeaponPickupAuthoring weaponPickup) =>
            {
                DeclareReferencedAsset(weaponPickup.PickedWeapon);
                DeclareAssetDependency(weaponPickup.gameObject, weaponPickup.PickedWeapon);
            });
        }

    }
    [UpdateAfter(typeof(WeaponConversionSystem))]
    public class WeaponPickupConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((WeaponPickupAuthoring weaponPickup) =>
            {
                var weaponEntity = GetPrimaryEntity(weaponPickup.PickedWeapon);
                var entity = GetPrimaryEntity(weaponPickup);
                DstEntityManager.AddComponentData(entity, new PickableWeapon { Entity = weaponEntity, SocketType = weaponPickup.PickedWeapon.SocketType });
                DstEntityManager.AddComponent<StatefulTriggerEvent>(entity);
            });
        }

    }

}
