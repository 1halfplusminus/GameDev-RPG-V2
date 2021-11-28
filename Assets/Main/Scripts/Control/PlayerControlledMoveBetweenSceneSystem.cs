
using RPG.Core;
using RPG.Saving;
using Unity.Entities;

namespace RPG.Control
{
    [GenerateAuthoringComponent]
    public struct InScene : IComponentData
    {
        public Hash128 SceneGUID;
    }
    public struct InSceneEntity : IComponentData
    {
        public Entity SceneEntity;
    }

    public struct InSceneLoaded : IComponentData
    {

    }
    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class PlayerControlledMoveBetweenSceneSystem : SystemBase
    {
        EntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithAll<PlayerControlled>()
            .ForEach((int entityInQueryIndex, Entity e, in TriggerSceneLoad triggerSceneLoad) =>
            {
                commandBuffer.AddComponent(entityInQueryIndex, e, new InScene() { SceneGUID = triggerSceneLoad.SceneGUID });
                commandBuffer.RemoveComponent<InSceneLoaded>(entityInQueryIndex, e);
            }).ScheduleParallel();
            Entities
            .WithAll<PlayerControlled>()
            .ForEach((int entityInQueryIndex, Entity e, in LoadSceneAsync loadSceneAsync) =>
            {
                commandBuffer.AddComponent(entityInQueryIndex, e, new InSceneEntity() { SceneEntity = loadSceneAsync.SceneEntity });
            }).ScheduleParallel();

            Entities
            .WithAll<PlayerControlled>()
            .WithNone<InSceneLoaded, LoadSceneAsync, InSceneEntity>()
            .ForEach((int entityInQueryIndex, Entity e, in InScene inScene) =>
            {
                commandBuffer.AddComponent(entityInQueryIndex, e, new TriggerSceneLoad { SceneGUID = inScene.SceneGUID });
                /*       commandBuffer.AddComponent<DontLoadSceneState>(entityInQueryIndex, e); */
            }).ScheduleParallel();

            Entities
           .WithAll<PlayerControlled, TriggeredSceneLoaded>()
           .WithNone<InSceneLoaded>()
           .ForEach((int entityInQueryIndex, Entity e) =>
           {
               commandBuffer.AddComponent<InSceneLoaded>(entityInQueryIndex, e);
               /*          commandBuffer.RemoveComponent<DontLoadSceneState>(entityInQueryIndex, e); */
           }).ScheduleParallel();
            commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}

