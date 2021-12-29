
using Unity.Entities;
using Unity.Jobs;
using RPG.Core;
using UnityEngine.Playables;
using RPG.Control;
using Cinemachine;
using UnityEngine;
using RPG.Hybrid;
using System.Collections.Generic;

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
    [DisableAutoCreation]
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class PlayableDeclareReferencedDirectorConversionSystem : GameObjectConversionSystem
    {
        HashSet<PlayableAsset> dones;
        protected override void OnCreate()
        {
            base.OnCreate();
            dones = new HashSet<PlayableAsset>();
        }
        protected override void OnUpdate()
        {
            dones.Clear();
            Entities.ForEach((PlayableDirector playableDirector) =>
           {
               DeclareAssetDependencyRecurse(playableDirector, playableDirector.playableAsset);
           });

        }
        protected void DeclareAssetDependencyRecurse(PlayableDirector playableDirector, PlayableAsset asset)
        {
            if (asset == null) { return; };
            if (dones.Contains(asset))
            {
                return;
            }
            dones.Add(asset);
            DeclareAssetDependency(playableDirector.gameObject, asset);
            DeclareReferencedAsset(asset);
            var outputs = asset.outputs;
            foreach (var output in outputs)
            {
                if (output.sourceObject is PlayableAsset playableAsset)
                {
                    DeclareAssetDependencyRecurse(playableDirector, playableAsset);

                }
            }
        }
    }

    [UpdateAfter(typeof(CinemachineCameraConversionSystem))]
    public class PlayableDirectorConversionSystem : GameObjectConversionSystem
    {
        HashSet<PlayableAsset> dones;
        EntityQuery query;
        protected override void OnCreate()
        {
            base.OnCreate();
            dones = new HashSet<PlayableAsset>();
            query = EntityManager.CreateEntityQuery(typeof(CinemachineBrain));
        }
        protected override void OnUpdate()
        {
            dones.Clear();
            CinemachineBrain brain = null;
            if (query.CalculateEntityCount() > 0)
            {
                Entity brainEntity = query.GetSingletonEntity();
                brain = EntityManager.GetComponentObject<CinemachineBrain>(brainEntity);
            }

            Entities.ForEach((PlayableDirector playableDirector) =>
            {
                AddHybridComponent(playableDirector);
                if (playableDirector.playableAsset == null) { return; }
                var entity = GetPrimaryEntity(playableDirector);
                LinkBrain(playableDirector, brain, entity);
                ConvertOutputRecurse(playableDirector, playableDirector.playableAsset);
            });

            /* Entities.ForEach((CinemachineTrack track) =>
            {
                Debug.Log("Convert CinemachineTrack");
            }); */
        }
        private void LinkBrain(PlayableDirector playableDirector, CinemachineBrain brain, Entity entity)
        {

            var outputs = playableDirector.playableAsset.outputs;
            foreach (var output in outputs)
            {

                if (output.sourceObject is CinemachineTrack)
                {
                    if (brain != null)
                    {
                        Debug.Log($"Set Generic Binding");
                        playableDirector.SetGenericBinding(output.sourceObject, brain);
                    }
                    else
                    {
                        DstEntityManager.AddComponent<LinkCinemachineBrain>(entity);
                        break;
                    }

                }
            }

        }
        private void ConvertOutputRecurse(PlayableDirector playableDirector, PlayableAsset asset)
        {
            if (dones.Contains(asset))
            {
                return;
            }
            dones.Add(asset);
            var outputs = asset.outputs;
            foreach (var output in outputs)
            {
                if (output.sourceObject is PlayableAsset playableAsset)
                {
                    if (HasPrimaryEntity(playableAsset))
                    {
                        var outputEntity = GetPrimaryEntity(playableAsset);
                        DstEntityManager.AddComponentObject(outputEntity, playableAsset);
                        ConvertOutputRecurse(playableDirector, playableAsset);
                    }

                }

            }
        }
    }
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class InitialiseHybridPayableDirectory : SystemBase
    {
        protected override void OnUpdate()
        {
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
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] {
                    typeof(PlayableDirector),
                },
                Any = new ComponentType[] {
                    typeof(CollidWithPlayer),
                    typeof(Playing),
                    typeof(TriggeredBy)
                },
                None = new ComponentType[] {
                    typeof(Played)
                }
            }));
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            var commandBufferP = commandBuffer.AsParallelWriter();
            Entities
            .WithAll<PlayableDirector>()
            .WithNone<Played, Playing, Spawned>()
            .ForEach((int entityInQueryIndex, Entity e, in CollidWithPlayer collidWithPlayer) =>
            {
                commandBufferP.AddComponent<Play>(entityInQueryIndex, e);
                commandBufferP.AddComponent(entityInQueryIndex, e, new TriggeredBy { Entity = collidWithPlayer.Entity });
                commandBufferP.AddComponent<DisabledControl>(entityInQueryIndex, collidWithPlayer.Entity);
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
                if (playableDirector.playableGraph.IsDone())
                {
                    var trigger = collidPlayer.Entity;
                    commandBuffer.RemoveComponent<Playing>(e);
                    commandBuffer.AddComponent<Played>(e);
                    commandBuffer.RemoveComponent<DisabledControl>(trigger);
                }
            }).WithoutBurst().Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

}
