
using Unity.Entities;
using RPG.Core;
using Unity.Animation.Hybrid;
using Unity.Transforms;
using RPG.Animation;
using Unity.Collections;

namespace RPG.Combat
{
    [UpdateAfter(typeof(RigConversion))]
    public class FighterConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((FighterAuthoring fighter) =>
            {
                var entity = GetPrimaryEntity(fighter);
                /*    CreateHitEvent(fighter, entity); */
                DstEntityManager.AddComponent<HittedByRaycast>(entity);

                //FIXME: Weapon value change be assigned when weapon is equiped
                DstEntityManager.AddComponentData(entity, new Fighter
                {
                    CurrentAttack = Attack.Create()
                });
                DstEntityManager.AddBuffer<HitEvent>(entity);
                DstEntityManager.AddComponent<LookAt>(entity);
                DstEntityManager.AddComponent<DeltaTime>(entity);
                var equipableSockets = new EquipableSockets
                {
                    LeftHandSocket = TryGetPrimaryEntity(fighter.LeftHandSocket),
                    RightHandSocket = TryGetPrimaryEntity(fighter.RightHandSocket)
                };
                DstEntityManager.AddComponentData(entity, equipableSockets);
                foreach (var socket in equipableSockets)
                {
                    DstEntityManager.AddComponentData(socket, new EquipedBy { Entity = entity });
                }
                var weaponEntity = GetPrimaryEntity(fighter.Weapon);
                var socketEntity = equipableSockets.GetSocketForType(fighter.Weapon.SocketType);
                if (socketEntity != Entity.Null)
                {
                    DstEntityManager.AddComponentData(weaponEntity, new EquipInSocket { Socket = socketEntity });
                }
            });
        }
        //FIXME: Weapon value change be assigned when weapon is equiped

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
                    if (fighter.RightHandSocket.gameObject.GetComponent<LateAnimationGraphWriteTransformHandle>() == null)
                    {
                        fighter.RightHandSocket.gameObject.AddComponent<LateAnimationGraphWriteTransformHandle>();
                    }

                    if (fighter.LeftHandSocket.gameObject.GetComponent<LateAnimationGraphWriteTransformHandle>() == null)
                    {
                        fighter.LeftHandSocket.gameObject.AddComponent<LateAnimationGraphWriteTransformHandle>();
                    }

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
#if UNITY_EDITOR
    public class WeaponConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((WeaponAsset weapon) =>
            {
                var weaponEntity = GetPrimaryEntity(weapon);
                var weaponPrefab = GetPrimaryEntity(weapon.WeaponPrefab);
                DstEntityManager.AddComponentData(weaponEntity, new EquippedPrefab { Value = weaponPrefab });
                DstEntityManager.AddComponentData(weaponEntity, new Weapon
                {
                    AttackDuration = weapon.AttackDuration,
                    Cooldown = weapon.Cooldown,
                    Damage = weapon.Damage,
                    Range = weapon.Range,
                    SocketType = weapon.SocketType,
                    HitEvents = CreateHitEvent(weapon)
                });
                if (weapon.Animation != null)
                {
                    var blobAsset = BlobAssetStore.GetClip(weapon.Animation);
                    DstEntityManager.AddComponentData(weaponEntity, new ChangeAttackAnimation { Animation = blobAsset });
                }
            });
        }
        private FixedList32<float> CreateHitEvent(WeaponAsset weapon)
        {
            var hitEvents = new FixedList32<float>();
            hitEvents.Length = weapon.HitEvents.Count;
            for (int i = 0; i < weapon.HitEvents.Count; i++)
            {
                hitEvents[i] = weapon.HitEvents[i];
            }
            return hitEvents;
        }
    }
#endif
}
