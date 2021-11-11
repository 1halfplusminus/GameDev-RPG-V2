
using Unity.Entities;
using Unity.Jobs;
using RPG.Core;
using UnityEngine.Playables;
using RPG.Control;
using Cinemachine;
using UnityEngine;

namespace RPG.Gameplay
{
    public struct TriggeredBy : IComponentData
    {
        public Entity Entity;
    }
    public struct Playing : IComponentData
    {

    }
    public struct Play : IComponentData
    {

    }
    public struct Played : IComponentData
    {

    }
    public class PlayableDirectorConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {

            Entities.ForEach((PlayableDirector playableDirector) =>
            {
                Entity brainEntity = DstEntityManager.CreateEntityQuery(typeof(CinemachineBrain)).GetSingletonEntity();
                var brain = DstEntityManager.GetComponentObject<CinemachineBrain>(brainEntity);
                var entity = GetPrimaryEntity(playableDirector);
                DstEntityManager.AddComponentObject(entity, playableDirector);
                if (brain != null)
                {
                    Debug.Log("Brain found");
                    var outputs = playableDirector.playableAsset.outputs;
                    foreach (var output in outputs)
                    {
                        if (output.sourceObject is CinemachineTrack cinemachineTrack)
                        {
                            Debug.Log("Set Generic Binding");
                            playableDirector.SetGenericBinding(output.sourceObject, brain);
                        }
                    }
                }

            });
        }
    }

    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public class TriggerTimelineSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var playerControlleds = GetComponentDataFromEntity<PlayerControlled>(true);
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            var commandBufferP = commandBuffer.AsParallelWriter();
            Entities
            .WithReadOnly(playerControlleds)
            .WithNone<Played, Playing>()
            .ForEach((int entityInQueryIndex, Entity e, in DynamicBuffer<StatefulTriggerEvent> statefulCollisionEvents) =>
            {
                for (int i = 0; i < statefulCollisionEvents.Length; i++)
                {
                    var collidWith = statefulCollisionEvents[i].GetOtherEntity(e);
                    if (playerControlleds.HasComponent(collidWith))
                    {
                        commandBufferP.AddComponent<Play>(entityInQueryIndex, e);
                        commandBufferP.AddComponent(entityInQueryIndex, e, new TriggeredBy { Entity = collidWith });
                        commandBufferP.AddComponent<Disabled>(entityInQueryIndex, collidWith);
                        return;
                    }
                }
            }).ScheduleParallel();

            Entities
            .WithNone<Played, Playing>()
            .WithAll<Play>()
            .ForEach((Entity e, PlayableDirector playableDirector) =>
            {
                playableDirector.Play();
                commandBuffer.RemoveComponent<Play>(e);
                commandBuffer.AddComponent<Playing>(e);
            }).WithoutBurst().Run();

            Entities
            .WithNone<Played>()
            .WithAll<Playing>()
            .ForEach((Entity e, PlayableDirector playableDirector, in TriggeredBy collidPlayer) =>
            {
                if (playableDirector.state != PlayState.Playing)
                {
                    var trigger = collidPlayer.Entity;
                    commandBuffer.RemoveComponent<Playing>(e);
                    commandBuffer.AddComponent<Played>(e);
                    commandBuffer.RemoveComponent<Disabled>(trigger);
                }
            }).WithoutBurst().Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

}
