
using Unity.Entities;
using Unity.Jobs;
using RPG.Core;
using UnityEngine.Playables;
using RPG.Control;
using Cinemachine;
using UnityEngine;
using RPG.Hybrid;

namespace RPG.Gameplay
{
    public struct LinkCinemachineBrain : IComponentData { }
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
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class PlayableDeclareReferencedDirectorConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PlayableDirector playableDirector) =>
           {
               DeclareAssetDependency(playableDirector.gameObject, playableDirector.playableAsset);
           });

        }
    }
    [UpdateAfter(typeof(CinemachineCameraConversionSystem))]
    public class PlayableDirectorConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            CinemachineBrain brain = null;
            for (int i = 0; i < World.All.Count; i++)
            {
                var world = World.All[i];
                var em = world.EntityManager;
                var query = em.CreateEntityQuery(typeof(CinemachineBrain));
                if (query.CalculateEntityCount() == 1)
                {
                    Entity brainEntity = query.GetSingletonEntity();
                    brain = em.GetComponentObject<CinemachineBrain>(brainEntity);
                    Debug.Log("Brain found");
                    break;
                }
            }

            Entities.ForEach((PlayableDirector playableDirector) =>
            {
                var entity = GetPrimaryEntity(playableDirector);
                /* DstEntityManager.AddComponentObject(entity, playableDirector); */
                AddHybridComponent(playableDirector);
                Debug.Log("Brain is not null");
                var outputs = playableDirector.playableAsset.outputs;
                foreach (var output in outputs)
                {
                    if (output.sourceObject is Component mono)
                    {
                        AddHybridComponent(mono);
                    }
                    if (output.sourceObject is CinemachineTrack cinemachineTrack)
                    {

                        if (brain != null)
                        {
                            Debug.Log("Set Generic Binding");
                            playableDirector.SetGenericBinding(output.sourceObject, brain);
                        }
                        else
                        {
                            DstEntityManager.AddComponent<LinkCinemachineBrain>(entity);
                        }

                    }

                }

            });
        }
    }
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class InitialiseHybridPayableDirectory : SystemBase
    {
        protected override void OnUpdate()
        {
            Debug.Log("InitialiseHybridPayableDirectory");
            if (!HasSingleton<CinemachineBrainTag>()) { return; }
            var cinemachineBrainEntity = GetSingletonEntity<CinemachineBrainTag>();
            var cinemachineBrain = EntityManager.GetComponentObject<CinemachineBrain>(cinemachineBrainEntity);
            Entities
            .WithChangeFilter<LinkCinemachineBrain>()
            .ForEach((PlayableDirector playableDirector) =>
            {
                var outputs = playableDirector.playableAsset.outputs;
                foreach (var output in outputs)
                {

                    if (output.sourceObject is CinemachineTrack cinemachineTrack)
                    {
                        Debug.Log("Link cinemachineBrain");
                        playableDirector.SetGenericBinding(output.sourceObject, cinemachineBrain);
                    }

                }

            }).WithoutBurst().Run();
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
            .WithAll<PlayableDirector>()
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
