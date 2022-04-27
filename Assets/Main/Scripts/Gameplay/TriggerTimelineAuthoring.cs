
using Unity.Entities;
using Unity.Jobs;
using RPG.Core;
using UnityEngine.Playables;
using RPG.Control;
using System.Collections.Generic;

namespace RPG.Gameplay
{

    public struct TriggeredBy : IComponentData
    {
        public Entity Entity;
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
            Entities.ForEach((PlayableDirector playableDirector) => DeclareAssetDependencyRecurse(playableDirector, playableDirector.playableAsset));
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

    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial class TriggerTimelineSystem : SystemBase
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
