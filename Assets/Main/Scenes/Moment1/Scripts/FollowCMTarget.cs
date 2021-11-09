
using RPG.Core;
using Unity.Entities;



[UpdateBefore(typeof(CoreSystemGroup))]
public class CMCameraSystem : SystemBase
{
    EntityCommandBufferSystem entityCommandBufferSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var commandBufferP = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        if (HasSingleton<CMTarget1>())
        {
            var target1 = GetSingletonEntity<CMTarget1>();
            Entities.WithAll<CMCamera1>().ForEach((int entityInQueryIndex, Entity e) =>
            {
                commandBufferP.AddComponent(entityInQueryIndex, e, new Follow { Entity = target1 });
                commandBufferP.AddComponent(entityInQueryIndex, e, new LookAt { Entity = target1 });
            }).ScheduleParallel();
        }
        if (HasSingleton<CMTarget2>())
        {
            var target2 = GetSingletonEntity<CMTarget2>();
            Entities.WithAll<CMCamera2>().ForEach((int entityInQueryIndex, Entity e) =>
            {
                commandBufferP.AddComponent(entityInQueryIndex, e, new Follow { Entity = target2 });
                commandBufferP.AddComponent(entityInQueryIndex, e, new LookAt { Entity = target2 });
            }).ScheduleParallel();

        }
        entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

    }
}