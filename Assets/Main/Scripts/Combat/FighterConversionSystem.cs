
using Unity.Entities;
using RPG.Core;
using Unity.Animation.Hybrid;
using Unity.Transforms;

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
                var hitEvents = DstEntityManager.AddBuffer<HitEvent>(entity);
                foreach (var hit in fighter.HitEvents)
                {
                    hitEvents.Add(new HitEvent { Time = hit });
                }
                DstEntityManager.AddComponent<HittedByRaycast>(entity);
                DstEntityManager.AddComponentData(entity, new Fighter
                {
                    WeaponRange = fighter.WeaponRange,
                    AttackCooldown = fighter.AttackCooldown,
                    AttackDuration = fighter.AttackDuration,
                    WeaponDamage = fighter.WeaponDamage,
                    CurrentAttack = Attack.Create()
                });
                DstEntityManager.AddComponent<LookAt>(entity);
                DstEntityManager.AddComponent<DeltaTime>(entity);

                var weaponPrefabEntity = TryGetPrimaryEntity(fighter.WeaponPrefab);
                if (weaponPrefabEntity != Entity.Null)
                {
                    var weaponSocketEntity = GetPrimaryEntity(fighter.WeaponSocket);
                    var weaponSpawnerEntity = CreateAdditionalEntity(fighter);
                    DstEntityManager.AddComponentData(weaponSpawnerEntity, new Spawn() { Prefab = weaponPrefabEntity, Parent = weaponSocketEntity });
                    DstEntityManager.AddComponentData(weaponSpawnerEntity, new LocalToWorld { Value = fighter.WeaponPrefab.transform.localToWorldMatrix });
                    /*    DstEntityManager.AddComponentData(weaponSpawnerEntity, new LocalToWorld { Value = fighter.WeaponSocket.transform.localToWorldMatrix }); */
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
                if (fighter.WeaponPrefab != null)
                {
                    DeclareReferencedPrefab(fighter.WeaponPrefab);
                }

            });
        }
    }
}
