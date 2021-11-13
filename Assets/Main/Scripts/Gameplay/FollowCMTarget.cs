
using RPG.Core;
using Unity.Entities;


namespace RPG.Gameplay
{
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
            var hasAnyTarget = false;
            var commandBufferP = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            if (GetTarget<CMTarget1>(out var cmTarget1))
            {
                Entities.WithAll<CMCamera1>().ForEach((int entityInQueryIndex, Entity e) =>
                {
                    AddFollowComponent(commandBufferP, entityInQueryIndex, e, cmTarget1);
                }).ScheduleParallel();
                hasAnyTarget = true;
            }
            if (GetTarget<CMTarget2>(out var cmTarget2))
            {
                Entities.WithAll<CMCamera2>().ForEach((int entityInQueryIndex, Entity e) =>
                {
                    AddFollowComponent(commandBufferP, entityInQueryIndex, e, cmTarget2);
                }).ScheduleParallel();
                hasAnyTarget = true;
            }
            if (GetTarget<CMTarget3>(out var cmTarget3))
            {
                Entities.WithAll<CMCamera3>().ForEach((int entityInQueryIndex, Entity e) =>
                {
                    commandBufferP.AddComponent(entityInQueryIndex, e, new LookAt { Entity = cmTarget3 });
                }).ScheduleParallel();
                hasAnyTarget = true;
            }
            if (hasAnyTarget)
            {
                entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            }

        }

        private bool GetTarget<T>(out Entity target)
        {
            target = Entity.Null;
            var hasTarget = HasSingleton<T>();
            if (hasTarget)
            {
                target = GetSingletonEntity<T>();
                return true;
            }
            return hasTarget;
        }

        private static void AddFollowComponent(EntityCommandBuffer.ParallelWriter commandBufferP, int entityInQueryIndex, Entity e, Entity target)
        {
            commandBufferP.AddComponent(entityInQueryIndex, e, new Follow { Entity = target });
            commandBufferP.AddComponent(entityInQueryIndex, e, new LookAt { Entity = target });
        }
    }
}