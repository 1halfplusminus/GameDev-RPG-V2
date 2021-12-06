
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
                    //FIXME: Weapon value change be assigned when weapon is equiped
                    /* Range = fighter.Weapon.Range,
                    Cooldown = fighter.Weapon.Cooldown,
                    AttackDuration = fighter.Weapon.AttackDuration,
                    Damage = fighter.Weapon.Damage, */
                    CurrentAttack = Attack.Create()
                });
                DstEntityManager.AddComponent<LookAt>(entity);
                DstEntityManager.AddComponent<DeltaTime>(entity);
                var weaponEntity = GetPrimaryEntity(fighter.Weapon);
                var socketEntity = GetPrimaryEntity(fighter.WeaponSocket);

                DstEntityManager.AddComponentData(weaponEntity, new EquipInSocket { Socket = socketEntity });
                DstEntityManager.AddComponentData(socketEntity, new EquipedBy { Entity = entity });
                DstEntityManager.AddBuffer<HitEvent>(entity);
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
}
