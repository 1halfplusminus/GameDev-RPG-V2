using Unity.Entities;

namespace RPG.Core
{
    [GenerateAuthoringComponent]
    public struct MaxLifeTime : IComponentData
    {
        public float Value;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    public class MaxLifeTimeSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {

            var ecb = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.WithAll<MaxLifeTime>().WithNone<DeltaTime>().ForEach((int entityInQueryIndex, Entity e) =>
            {
                ecb.AddComponent<DeltaTime>(entityInQueryIndex, e);
            }).ScheduleParallel();
            Entities.ForEach((int entityInQueryIndex, Entity e, ref MaxLifeTime lifeTime, in DeltaTime delta) =>
            {
                lifeTime.Value -= delta.Value;
                if (lifeTime.Value <= 0)
                {
                    ecb.DestroyEntity(entityInQueryIndex, e);
                }
            }).ScheduleParallel();
        }
    }
}