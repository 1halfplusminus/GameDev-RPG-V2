
using UnityEngine;
using Unity.Entities;

using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

using Unity.Burst;
using Unity.Animation;
using Unity.Deformations;


namespace RPG.Animation
{
    public class ComputeDeformation : ComputeDeformationDataBase
    {

    }
    public struct AnimatedEntity : IBufferElementData
    {
        public Entity Value;
    }

    public struct AnimationClips : IBufferElementData
    {
        public BlobAssetReference<Clip> Clip;
        public BlobAssetReference<ClipInstance> ClipInstance;
    }

    public struct CurrentlyPlayingClip : IBufferElementData
    {
        public FixedString32Bytes Name;
        public float Weight;
    }

    public struct AnimationStreamComponent : IComponentData
    {
        public AnimationStream Value;
    }
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    public partial class AnimationStateSystem : SystemBase
    {
        struct CreateAnimationClipInstance : IJobChunk
        {
            public BufferTypeHandle<AnimationClips> AnimationClipHandle;
            public ComponentTypeHandle<Rig> RigHandle;
            public uint LastSystemVersion;
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {

                if (
                    !chunk.DidChange(AnimationClipHandle, LastSystemVersion)
                )
                {
                    return;
                }
                var rigs = chunk.GetNativeArray(RigHandle);
                var clipsBufferAccessor = chunk.GetBufferAccessor(AnimationClipHandle);
                for (int i = 0; i < rigs.Length; i++)
                {
                    var clips = clipsBufferAccessor[i];
                    for (int j = 0; j < clips.Length; j++)
                    {
                        var clip = clips[j];
                        if (!clip.ClipInstance.IsCreated)
                        {
                            var clipInstance = ClipManager.Instance.GetClipFor(rigs[i].Value, clip.Clip);
                            clip.ClipInstance = clipInstance;
                            clips[j] = clip;
                        }
                    }
                }
                rigs.Dispose();
            }
        }
        EntityQuery query;
        protected override void OnCreate()
        {
            base.OnCreate();
            query = GetEntityQuery(typeof(Rig), typeof(AnimationClips));
            // query.SetChangedVersionFilter(ComponentType.ReadWrite<AnimationClips>());
            RequireForUpdate(query);
        }
        protected override void OnUpdate()
        {
            var chunkIterator = query.GetArchetypeChunkIterator();
            var createAnimationClips = new CreateAnimationClipInstance
            {
                AnimationClipHandle = GetBufferTypeHandle<AnimationClips>(),
                RigHandle = GetComponentTypeHandle<Rig>(),
                LastSystemVersion = this.LastSystemVersion
            };
            createAnimationClips.RunWithoutJobs(ref chunkIterator);

        }
    }

    [BurstCompile]
    public partial struct AnimationCoreSystem : ISystem
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        public void OnCreate(ref SystemState state)
        {
            entityCommandBufferSystem = state.World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        public void OnDestroy(ref SystemState state)
        {

        }

        public void OnUpdate(ref SystemState state)
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            state.Entities
            .WithNone<AnimationStreamComponent>().ForEach((
                int entityInQueryIndex,
                Entity e,
                ref DynamicBuffer<AnimatedData> animatedDatas,
                in Rig RigRef) =>
            {
                cbp.AddComponent(entityInQueryIndex, e, new AnimationStreamComponent
                {
                    Value = AnimationStream.Create(RigRef.Value, animatedDatas.AsNativeArray())
                });
            }).ScheduleParallel();
            var localToWorlds = state.GetComponentDataFromEntity<LocalToWorld>(true);
            state.Entities
            .WithReadOnly(localToWorlds)
            .ForEach((ref RigRootEntity rigRootEntity) =>
            {
                var localToWorld = localToWorlds[rigRootEntity.Value];
                // var rootTransform = mathex.AffineTransform(localToWorld.Value);
                // rigRootEntity.RemapToRootMatrix = rootTransform;
            }).ScheduleParallel();

            // state
            // .Entities
            // .ForEach((int entityInQueryIndex, ref DynamicBuffer<AnimatedEntity> animatedEntities, in AnimationStreamComponent streamComponent) =>
            // {

            //     var stream = streamComponent.Value;
            //     for (int i = 0; i < animatedEntities.Length; i++)
            //     {
            //         if (i < stream.RotationCount)
            //         {
            //             var rotation = stream.GetLocalToParentRotation(i);
            //             cbp.AddComponent(entityInQueryIndex, animatedEntities[i].Value, new Rotation { Value = rotation.value });
            //         }
            //         if (i < stream.TranslationCount)
            //         {
            //             var translation = stream.GetLocalToParentTranslation(i);
            //             cbp.AddComponent(entityInQueryIndex, animatedEntities[i].Value, new Translation { Value = translation });
            //         }
            //         if (i < stream.ScaleCount)
            //         {
            //             var scale = stream.GetLocalToParentScale(i);
            //             cbp.AddComponent(entityInQueryIndex, animatedEntities[i].Value, new CompositeScale { Value = float4x4.Scale(scale) });
            //         }
            //     }
            // }).ScheduleParallel();

            state
            .Entities
            .ForEach((ref DynamicBuffer<AnimatedLocalToRoot> animatedDatas, in AnimationStreamComponent streamComponent) =>
            {

                var stream = streamComponent.Value;
                for (int i = 0; i < animatedDatas.Length; i++)
                {
                    var localToRoot = stream.GetLocalToRootMatrix(i);
                    animatedDatas[i] = new AnimatedLocalToRoot { Value = localToRoot };
                }
            }).ScheduleParallel();

            var streams = state.GetComponentDataFromEntity<AnimationStreamComponent>();
            state.Entities.WithReadOnly(streams).ForEach((ref DynamicBuffer<BlendShapeWeight> shapeKeys, in RigEntity rigEntity) =>
            {
                if (streams.HasComponent(rigEntity.Value))
                {
                    var stream = streams[rigEntity.Value].Value;
                    for (int i = 0; i < stream.FloatCount; i++)
                    {
                        shapeKeys[i] = new BlendShapeWeight
                        {
                            Value = stream.GetFloat(i)
                        };
                    }
                }
            }).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(state.Dependency);
        }
    }
}
