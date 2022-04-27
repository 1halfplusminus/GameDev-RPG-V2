using Unity.Entities;
using RPG.Core;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine;
using RPG.Animation;
using Unity.Rendering;
using UnityEngine.VFX;
using RPG.Control;
using RPG.Stats;
using Unity.Collections;

namespace RPG.Combat {
[UpdateInGroup(typeof(CombatSystemGroup))]
public partial class CollidWithPickableWeaponSystem : SystemBase
{
        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery collidWithPickableweaponQuery;

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
            .WithNone<Picked>()
            .WithStoreEntityQueryInField(ref collidWithPickableweaponQuery)
            .ForEach((int entityInQueryIndex, Entity e, in CollidWithPlayer collidWithPlayer, in PickableWeapon picked) =>
            {
                cbp.AddComponent<Picked>(entityInQueryIndex, e);
                cbp.AddComponent(entityInQueryIndex, e, new HideForSecond { Time = 5f });
                // cbp.AddComponent(entityInQueryIndex, collidWithPlayer.Entity, new Equip { Equipable = picked.Entity, SocketType = picked.SocketType });
                cbp.RemoveComponent<StatefulTriggerEvent>(entityInQueryIndex, e);

            }).ScheduleParallel();

            Entities
            .WithAll<Picked>()
            .WithNone<DisableRendering>()
            .ForEach((int entityInQueryIndex, Entity e, in LocalToWorld localToWorld) =>
            {
                Debug.Log($"{e.Index} was picked");
                cbp.AddComponent<DisableRendering>(entityInQueryIndex, e);
                cbp.RemoveComponent<StatefulTriggerEvent>(entityInQueryIndex, e);

            }).ScheduleParallel();

            Entities
            .WithAll<Picked, UnHide>()
            .ForEach((int entityInQueryIndex, Entity e) =>
            {
                Debug.Log($"{e.Index} pick up respawn");
                cbp.RemoveComponent<DisableRendering>(entityInQueryIndex, e);
                cbp.AddBuffer<StatefulTriggerEvent>(entityInQueryIndex, e);
                cbp.RemoveComponent<Picked>(entityInQueryIndex, e);
            }).ScheduleParallel();

            Entities
            .WithAll<Picked, LocalToWorld>()
            .ForEach((int entityInQueryIndex, Entity e, VisualEffect visualEffect) =>
            {
                visualEffect.Stop();
            })
            .WithoutBurst()
            .Run();

            Entities
            .WithAll<Picked, UnHide>()
            .ForEach((int entityInQueryIndex, Entity e, VisualEffect visualEffect) =>
            {
                visualEffect.Play();
            })
            .WithoutBurst()
            .Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }
}
