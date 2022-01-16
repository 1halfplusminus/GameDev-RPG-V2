
using UnityEngine;
using Unity.Entities;
using RPG.Core;
using RPG.Stats;
using Unity.Mathematics;
using RPG.Control;
using Unity.Burst;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Physics;
namespace RPG.Gameplay
{
    [GenerateAuthoringComponent]
    public struct RestaureHealthPercent : IComponentData
    {
        public float Value;

        public float GetNewHealth(Health currentHealth, BaseStats baseStats)
        {
            var newHealth = baseStats.ProgressionAsset.Value.GetStat(Stats.Stats.Health, baseStats.Level);
            var regen = newHealth * (Value / 100);
            return math.max(currentHealth.Value, regen);
        }
        public void RestaureHealth(ref Health currentHealth, BaseStats baseStats)
        {
            var maxHealth = baseStats.ProgressionAsset.Value.GetStat(Stats.Stats.Health, baseStats.Level);
            var regen = maxHealth * (Value / 100);
            currentHealth.Value = math.min(currentHealth.Value + regen, maxHealth);
        }
    }
    public struct Picking : IComponentData { }
    public struct Picked : IComponentData { }
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public class HealthPickupSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery pickingEntityQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            pickingEntityQuery = GetEntityQuery(typeof(HealthPickup), typeof(Picking));

        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            cb.RemoveComponentForEntityQuery<Picking>(pickingEntityQuery);
            Entities
            .WithNone<Picked>()
            .ForEach((int entityInQueryIndex, Entity e, in CollidWithPlayer collidWithPlayer, in RestaureHealthPercent restaureHealthPercent) =>
            {
                if (HasComponent<Health>(collidWithPlayer.Entity) && HasComponent<BaseStats>(collidWithPlayer.Entity))
                {
                    var basestats = GetComponent<BaseStats>(collidWithPlayer.Entity);
                    var health = GetComponent<Health>(collidWithPlayer.Entity);
                    restaureHealthPercent.RestaureHealth(ref health, basestats);
                    cbp.AddComponent(entityInQueryIndex, collidWithPlayer.Entity, health);
                    cbp.AddComponent<Picked>(entityInQueryIndex, e);
                    cbp.AddComponent<Picking>(entityInQueryIndex, e);
                    Log(e, health.Value);
                }

            }).ScheduleParallel();

            Entities
            .WithAll<Picking, HealthPickup>()
            .ForEach((in HealingAudio healingAudio) =>
            {
                var audioSourceEntity = EntityManager.GetComponentObject<AudioSource>(healingAudio.Entity);
                audioSourceEntity.Play();
            })
            .WithoutBurst()
            .Run();

            Entities
            .WithAll<Picked>()
            .WithNone<DisableRendering>()
            .ForEach((int entityInQueryIndex, Entity e, DynamicBuffer<Child> children) =>
            {
                for (int i = 0; i < children.Length; i++)
                {
                    cbp.AddComponent<Picked>(entityInQueryIndex, children[i].Value);
                }
            }).ScheduleParallel();

            Entities
            .WithAll<Picked>()
            .WithNone<DisableRendering>()
            .ForEach((int entityInQueryIndex, Entity e) =>
            {
                cbp.AddComponent<DisableRendering>(entityInQueryIndex, e);
                // cbp.AddComponent<Disabled>(entityInQueryIndex, e);
            }).ScheduleParallel();

            Entities
            .WithNone<InteractWithUI>()
            .ForEach((ref VisibleCursor cursor, in DynamicBuffer<HittedByRaycastEvent> rayHits) =>
            {
                foreach (var rayHit in rayHits)
                {
                    if (rayHit.Hitted != Entity.Null)
                    {
                        if (HasComponent<RestaureHealthPercent>(rayHit.Hitted))
                        {
                            cursor.Cursor = CursorType.Health;
                        }
                    }
                }
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }


        private static void Log(Entity e, float newHealth)
        {
            Debug.Log($"Restaure {newHealth} health for ${e.Index}");
        }
    }
}
