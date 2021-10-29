using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Combat
{

    [UpdateInGroup(typeof(CombatSystemGroup))]
    // Todo: Create multiple system group for combat Begin and End of combat Update in end of combat
    [UpdateAfter(typeof(DamageSystem))]
    public class HealthSystem : SystemBase
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
            var healths = GetComponentDataFromEntity<Health>(true);
            Entities
            .WithReadOnly(healths)
            .ForEach((int entityInQueryIndex, Entity entity, in Hit hit) =>
            {
                if (healths.HasComponent(hit.Hitted))
                {
                    var health = healths[hit.Hitted];
                    health.Value -= hit.Damage;
                    health.Value = math.max(health.Value, 0);
                    commandBuffer.SetComponent(entityInQueryIndex, hit.Hitted, health);

                }
            }).ScheduleParallel();
        }
    }
}