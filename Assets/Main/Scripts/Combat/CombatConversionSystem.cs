
using Unity.Entities;
using RPG.Core;
using Unity.Animation.Hybrid;
using Unity.Transforms;
using RPG.Animation;
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
                CreateHitEvent(fighter, entity);
                DstEntityManager.AddComponent<HittedByRaycast>(entity);

                //FIXME: Weapon value change be assigned when weapon is equiped
                DstEntityManager.AddComponentData(entity, new Fighter
                {
                    Range = fighter.Weapon.Range,
                    Cooldown = fighter.Weapon.Cooldown,
                    AttackDuration = fighter.Weapon.AttackDuration,
                    Damage = fighter.Weapon.Damage,
                    CurrentAttack = Attack.Create()
                });
                DstEntityManager.AddComponent<LookAt>(entity);
                DstEntityManager.AddComponent<DeltaTime>(entity);
                var weaponEntity = GetPrimaryEntity(fighter.Weapon);
                var socketEntity = GetPrimaryEntity(fighter.WeaponSocket);
                DstEntityManager.AddComponent<LateAnimationGraphWriteTransformHandle>(socketEntity);
                DstEntityManager.AddComponentData(weaponEntity, new EquipWeapon { Socket = socketEntity });
                DstEntityManager.AddComponentData(socketEntity, new EquipedBy { Entity = entity });
            });
        }
        //FIXME: Weapon value change be assigned when weapon is equiped
        private void CreateHitEvent(FighterAuthoring fighter, Entity entity)
        {
            var hitEvents = DstEntityManager.AddBuffer<HitEvent>(entity);
            foreach (var hit in fighter.Weapon.HitEvents)
            {
                hitEvents.Add(new HitEvent { Time = hit });
            }
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
                DstEntityManager.AddComponentData(weaponEntity, new WeaponPrefab { Value = weaponPrefab });
                if (weapon.Animation != null)
                {
                    var blobAsset = BlobAssetStore.GetClip(weapon.Animation);
                    DstEntityManager.AddComponentData(weaponEntity, new ChangeAttackAnimation { Animation = blobAsset });
                }
            });
        }


    }
}
