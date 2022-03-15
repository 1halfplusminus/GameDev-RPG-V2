using RPG.Core;
using Unity.Animation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace RPG.Combat
{
    public struct Died : IComponentData
    {

    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class KillCharacterSystem : SystemBase
    {
        EntityCommandBufferSystem commandBufferSystem;
        EntityQuery diedQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            diedQuery = GetEntityQuery(typeof(Died));
        }
        protected override void OnUpdate()
        {
            var cb = commandBufferSystem.CreateCommandBuffer();
            var cbp = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            cb.RemoveComponentForEntityQuery<Died>(diedQuery);
            Entities
            .WithNone<IsDeadTag>()
            .ForEach((Entity e, int entityInQueryIndex, in Health health) =>
            {
                if (health.Value <= 0)
                {
                    cbp.AddComponent<IsDeadTag>(entityInQueryIndex, e);
                    cbp.AddComponent<Died>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();

            Entities.WithAll<IsDeadTag, Hittable>()
            .ForEach((Entity e, int entityInQueryIndex) =>
            {
                cbp.RemoveComponent<Hittable>(entityInQueryIndex, e);
            })
            .ScheduleParallel();

            Entities
            .WithAll<IsDeadTag>()
            .WithNone<PhysicsExclude>()
            .ForEach((Entity e, int entityInQueryIndex) =>
            {
                cbp.AddComponent<PhysicsExclude>(entityInQueryIndex, e);
            })
            .ScheduleParallel();
            Entities
            .WithAll<IsDeadTag>()
            .WithNone<PhysicsExclude>()
            .ForEach((Entity e, int entityInQueryIndex, DynamicBuffer<Child> children) =>
            {
                for (var i = 0; i < children.Length; i++)
                {
                    cbp.AddComponent<PhysicsExclude>(entityInQueryIndex, children[i].Value);
                }

            })
            .ScheduleParallel();
            commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class DeadAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithAny<IsDeadTag>()
            .ForEach((ref CharacterAnimation animation) =>
            {

                animation.Dead += 0.02f;
                animation.Dead = math.min(animation.Dead, 1f);
            }).ScheduleParallel();
        }
    }
}