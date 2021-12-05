using Unity.Entities;
using RPG.Core;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine;
using RPG.Animation;

namespace RPG.Combat
{
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class EquipWeaponSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            Entities

            .WithChangeFilter<EquipWeapon>()
            .ForEach((int entityInQueryIndex, Entity e, in EquipWeapon equipWeapon, in WeaponPrefab prefab, in ChangeAttackAnimation changeAttackAnimation) =>
            {
                Debug.Log($"equip {e} in socket : ${equipWeapon.Socket}");
                cbp.AddComponent(entityInQueryIndex, equipWeapon.Socket, new SpawnWeapon { Prefab = prefab.Value, Animation = changeAttackAnimation.Animation });
            }).ScheduleParallel();

            Entities
            .ForEach((int entityInQueryIndex, Entity e, in SpawnWeapon spawn, in LocalToWorld localToWorld, in EquipedBy equipedBy) =>
            {
                Debug.Log($"spawn weapon prefab {spawn.Prefab.Index} at : ${localToWorld.Position}");
                if (spawn.Prefab != Entity.Null)
                {
                    cbp.AddComponent(entityInQueryIndex, e, new Spawn() { Prefab = spawn.Prefab, Parent = e });
                }
                cbp.AddComponent(entityInQueryIndex, equipedBy.Entity, new ChangeAttackAnimation() { Animation = spawn.Animation });
                cbp.RemoveComponent<SpawnWeapon>(entityInQueryIndex, e);
            }).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }
}
