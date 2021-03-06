using RPG.Core;
using Unity.Animation;
using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Combat
{

    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class KillCharacterSystem : SystemBase
    {
        EntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            Entities
            .WithNone<IsDeadTag>()
            .ForEach((Entity e, int entityInQueryIndex, in Health health) =>
            {
                if (health.Value <= 0)
                {
                    commandBuffer.AddComponent<IsDeadTag>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();

            Entities.WithAll<IsDeadTag, Hittable>()
            .ForEach((Entity e, int entityInQueryIndex) =>
            {

                commandBuffer.RemoveComponent<Hittable>(entityInQueryIndex, e);
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