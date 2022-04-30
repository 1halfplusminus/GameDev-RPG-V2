using Cinemachine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using RPG.Core;
using RPG.Hybrid;
using System.Reflection;
using System;
using System.Linq;
using Unity.Collections;

namespace RPG.Hybrid
{

    public struct TransformProxy : IComponentData
    {
        public Entity ProxyFor;
    }
    public struct VirtualCamera : IComponentData
    {
        public Entity FollowProxy;
        public Entity LookAtProxy;
    }
    public struct RebuildHierachy : IComponentData
    {
        public float4x4 LocalToWorld;
    }


    public struct FollowedBy : IComponentData
    {
        public Entity Entity;
    }

    public struct LookAtBy : IComponentData
    {
        public Entity Entity;
    }

    public struct CinemachineBrainTag : IComponentData
    {
    }
    public class CinemachineCameraConversionSystem : GameObjectConversionSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            this.AddTypeToCompanionWhiteList(typeof(CinemachineVirtualCamera));
            this.AddTypeToCompanionWhiteList(typeof(CinemachinePipeline));
            this.AddTypeToCompanionWhiteList(typeof(TransformProxyAuthoring));
            foreach (Type type in
                Assembly.GetAssembly(typeof(CinemachineComponentBase)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(CinemachineComponentBase))))
            {
                this.AddTypeToCompanionWhiteList(type);
            }
        }
        // TODO: Clean up put all follow target in a same parent game object
        protected override void OnUpdate()
        {
            Entities.ForEach((CinemachineBrain brain) =>
            {
                var brainEntity = TryGetPrimaryEntity(brain);
                DstEntityManager.AddComponentObject(brainEntity, brain);
                DstEntityManager.AddComponentData(brainEntity, new CinemachineBrainTag() { });
            });

            Entities.ForEach((CinemachineVirtualCamera virtualCamera) =>
            {
                var virtualCameraEntity = TryGetPrimaryEntity(virtualCamera);
                var virtualCameraComponent = new VirtualCamera { };
                DstEntityManager.AddComponent<CopyTransformFromGameObject>(virtualCameraEntity);
                if (virtualCameraEntity != Entity.Null)
                {
                    DstEntityManager.AddComponentObject(virtualCameraEntity, virtualCamera);
                    // DstEntityManager.AddComponentObject(virtualCameraEntity, virtualCamera.GetComponent<Transform>());
                    DeclareLinkedEntityGroup(virtualCamera.gameObject);
                    LoadCinemachineComponents(virtualCamera);
                    if (virtualCamera.m_Follow != null)
                    {
                        var followedProxyEntity = TryGetPrimaryEntity(virtualCamera.m_Follow.gameObject);
                        var transformProxy = virtualCamera.m_Follow.GetComponent<TransformProxyAuthoring>();
                        var followedEntity = transformProxy != null && transformProxy.Target != null ? TryGetPrimaryEntity(transformProxy.Target) : followedProxyEntity;
                        if (followedEntity != Entity.Null)
                        {
                            Debug.Log("Follow " + followedEntity.Index);
                            DstEntityManager.AddComponentData(virtualCameraEntity, new Follow() { Entity = followedEntity });
                        }
                        if (transformProxy != null)
                        {
                            DstEntityManager.AddComponentObject(followedProxyEntity, transformProxy);
                            // DstEntityManager.AddComponentData(followedProxyEntity, new Parent { Value = followedEntity });
                            // DstEntityManager.AddComponent<LocalToParent>(followedProxyEntity);
                        }
                        virtualCameraComponent.FollowProxy = followedProxyEntity;
                    }
                    if (virtualCamera.m_LookAt != null)
                    {
                        var lookAtProxyEntity = TryGetPrimaryEntity(virtualCamera.m_LookAt.gameObject);
                        var transformProxy = virtualCamera.m_LookAt.GetComponent<TransformProxyAuthoring>();
                        var lookAtEntity = transformProxy != null && transformProxy.Target != null ? TryGetPrimaryEntity(transformProxy.Target) : lookAtProxyEntity;
                        if (lookAtProxyEntity != Entity.Null)
                        {
                            Debug.Log("Look At " + lookAtProxyEntity.Index);
                            DstEntityManager.AddComponentData(virtualCameraEntity, new LookAt() { Entity = lookAtProxyEntity });
                        }
                        if (transformProxy != null)
                        {
                            DstEntityManager.AddComponentObject(lookAtProxyEntity, transformProxy);
                        }
                        virtualCameraComponent.LookAtProxy = lookAtProxyEntity;
                    }
                    DstEntityManager.AddComponentData(virtualCameraEntity, virtualCameraComponent);
                }

            });
            Entities.ForEach((CinemachinePipeline pipeline) =>
            {
                ConvertPipeline(this, DstEntityManager, pipeline);
            });
        }

        private void LoadCinemachineComponents(CinemachineVirtualCamera virtualCamera)
        {

            foreach (Transform child in virtualCamera.transform)
            {
                var pipeline = child.GetComponent<CinemachinePipeline>();
                if (pipeline != null)
                {
                    DeclareDependency(virtualCamera.gameObject, pipeline.gameObject);
                }
            }


        }

        public static Entity ConvertPipeline(CinemachineCameraConversionSystem conversionsSystem, EntityManager DstEntityManager, CinemachinePipeline pipeline)
        {
            var pipelineEntity = conversionsSystem.TryGetPrimaryEntity(pipeline);
            if (pipelineEntity != Entity.Null)
            {
                conversionsSystem.DeclareLinkedEntityGroup(pipeline.gameObject);
                DstEntityManager.AddComponentObject(pipelineEntity, pipeline);
                // DstEntityManager.AddComponentObject(pipelineEntity,pipeline.GetComponent<Transform>());
                CinemachineComponentBase[] components = pipeline.GetComponents<CinemachineComponentBase>();

                foreach (CinemachineComponentBase c in components)
                {
                    DstEntityManager.AddComponentObject(pipelineEntity, c);
                }
                conversionsSystem.DstEntityManager.AddComponentData(pipelineEntity, new RebuildHierachy { LocalToWorld = pipeline.transform.parent.localToWorldMatrix });

            }
            return pipelineEntity;
        }
    }
    [UpdateBefore(typeof(CinemachineVirtualCameraHybridSystem))]
    public partial class CinemachineVirtualCameraSpawnerFollow : SystemBase
    {

        protected override void OnUpdate()
        {
            Entities
            .WithNone<IsFollowingTarget>()
            .WithAll<CinemachineVirtualCamera>()
            .ForEach((ref Follow follow) =>
            {
                var hasSpawnComponents = GetComponentDataFromEntity<HasSpawn>(true);
                if (hasSpawnComponents.HasComponent(follow.Entity))
                {

                    follow.Entity = hasSpawnComponents[follow.Entity].Entity;
                }
            }).ScheduleParallel();
            Entities
            .WithAll<CinemachineVirtualCamera>()
            .WithNone<IsLookingAtTarget>()
            .ForEach((ref LookAt follow) =>
            {
                var hasSpawnComponents = GetComponentDataFromEntity<HasSpawn>(true);
                if (hasSpawnComponents.HasComponent(follow.Entity))
                {

                    follow.Entity = hasSpawnComponents[follow.Entity].Entity;
                }
            }).ScheduleParallel();

        }
    }
    [UpdateAfter(typeof(CoreSystemGroup))]
    public partial class CinemachineVirtualCameraHybridSystem : SystemBase
    {
        public struct UpdateProxyJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<Follow> FollowComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<LookAt> LookAtComponentTypeHandle;
            public ComponentTypeHandle<VirtualCamera> VirtualCameraTypeHandle;
            public uint LastSystemVersion;
            public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {

                if (chunk.DidChange(LookAtComponentTypeHandle, LastSystemVersion))
                {
                    var virtualCameras = chunk.GetNativeArray(VirtualCameraTypeHandle);
                    var lookAts = chunk.GetNativeArray(LookAtComponentTypeHandle);
                    for (int i = 0; i < lookAts.Length; i++)
                    {
                        if (virtualCameras[i].LookAtProxy != Entity.Null)
                        {
                            EntityCommandBuffer.AddComponent(
                                                        chunkIndex,
                                                        virtualCameras[i].LookAtProxy,
                                                        new Parent { Value = lookAts[i].Entity }
                                                    );
                            EntityCommandBuffer.AddComponent<LocalToParent>(
                               chunkIndex,
                               virtualCameras[i].LookAtProxy
                           );
                        }
                    }
                }
                if (chunk.DidChange(FollowComponentTypeHandle, LastSystemVersion))
                {
                    var virtualCameras = chunk.GetNativeArray(VirtualCameraTypeHandle);
                    var follows = chunk.GetNativeArray(LookAtComponentTypeHandle);
                    for (int i = 0; i < follows.Length; i++)
                    {
                        if (virtualCameras[i].FollowProxy != Entity.Null)
                        {
                            EntityCommandBuffer.AddComponent(
                                                        chunkIndex,
                                                        virtualCameras[i].FollowProxy,
                                                        new Parent { Value = follows[i].Entity }
                                                    );
                            EntityCommandBuffer.AddComponent<LocalToParent>(
                               chunkIndex,
                               virtualCameras[i].FollowProxy
                           );
                        }
                    }
                }
            }
        }
        EntityQuery updateProxyQuery;
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
            updateProxyQuery = GetEntityQuery(
                new ComponentType[] {
                    ComponentType.ReadOnly<Follow>(),
                    ComponentType.ReadOnly<LookAt>(),
                    ComponentType.ReadOnly<VirtualCamera>()
                }
            );
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            Dependency = new UpdateProxyJob
            {
                EntityCommandBuffer = commandBuffer.AsParallelWriter(),
                LastSystemVersion = LastSystemVersion,
                FollowComponentTypeHandle = GetComponentTypeHandle<Follow>(true),
                LookAtComponentTypeHandle = GetComponentTypeHandle<LookAt>(true),
                VirtualCameraTypeHandle = GetComponentTypeHandle<VirtualCamera>()
            }.ScheduleParallel(updateProxyQuery, Dependency);
            Entities
            .WithoutBurst()
            .WithChangeFilter<Follow>()
            .ForEach((Entity e, CinemachineVirtualCamera camera, in Follow target) =>
            {
                var transform = GetTransform(target.Entity, EntityManager);
                if (transform != null)
                {
                    camera.m_Follow = transform;
                    commandBuffer.AddComponent<IsFollowingTarget>(e);
                }
                else
                {
                    commandBuffer.RemoveComponent<IsFollowingTarget>(e);
                }
            }).Run();
            Entities
           .WithoutBurst()
           .WithChangeFilter<LookAt>()
           .ForEach((Entity e, CinemachineVirtualCamera camera, in LookAt target, in VirtualCamera cameraData) =>
           {
               var transform = GetTransform(cameraData.FollowProxy, EntityManager);
               if (transform != null)
               {
                   camera.m_LookAt = transform;
                   commandBuffer.AddComponent<IsLookingAtTarget>(e);
               }
               else
               {
                   commandBuffer.RemoveComponent<IsLookingAtTarget>(e);
               }

           }).Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        private static Transform GetTransform(Entity entity, EntityManager em)
        {
            if (em.HasComponent<HasSpawn>(entity))
            {
                return GetTransform(em.GetComponentData<HasSpawn>(entity).Entity, em);
            }
            if (em.HasComponent<NavMeshAgent>(entity))
            {
                return em.GetComponentObject<NavMeshAgent>(entity).transform;
            }
            if (em.HasComponent<CompanionLink>(entity))
            {
                return em.GetComponentObject<CompanionLink>(entity).Companion.transform;
            }

            return null;
        }
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class RebuildGameObjectHierachySystem : SystemBase
{
    struct TransformStash
    {
        public float3 position;
        public quaternion rotation;
    }
    EntityCommandBufferSystem entityCommandBufferSystem;
    EntityQuery rebuildHierachyQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var em = EntityManager;
        var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        Entities
        .WithChangeFilter<RebuildHierachy>()
        .WithStoreEntityQueryInField(ref rebuildHierachyQuery)
        .ForEach((Entity e, CompanionLink cp, in Parent parent, in RebuildHierachy rebuildHierachy) =>
        {

            if (EntityManager.HasComponent<CompanionLink>(parent.Value))
            {
                var t = cp.Companion.transform;
                var parentTransform = EntityManager.GetComponentObject<CompanionLink>(parent.Value);
                t.parent = parentTransform.Companion.transform;
                t.transform.position = rebuildHierachy.LocalToWorld.c3.xyz;
                commandBuffer.RemoveComponent<RebuildHierachy>(e);
                commandBuffer.RemoveComponent<EditorRenderData>(e);
            }
        }).WithoutBurst().Run();
        entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}