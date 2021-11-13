using Cinemachine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using RPG.Core;
using System.Collections.Generic;
using RPG.Hybrid;
using static Unity.Transforms.LocalToWorld;
using UnityEngine.SceneManagement;

namespace RPG.Hybrid
{
    public struct RebuildHierachy : IComponentData
    {
        public float4x4 LocalToWorld;
    }

    public struct IsFollowingTarget : IComponentData { }

    public struct IsLookingAtTarget : IComponentData { }
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
        // TODO: Clean up put all follow target in a same parent game object
        protected override void OnUpdate()
        {
            Entities.ForEach((CinemachineBrain brain) =>
            {
                var brainEntity = DstEntityManager.CreateEntity();
                DstEntityManager.AddComponentObject(brainEntity, brain);
                DstEntityManager.AddComponentData(brainEntity, new CinemachineBrainTag() { });
            });

            Entities.ForEach((CinemachineVirtualCamera virtualCamera) =>
            {

                var virtualCameraEntity = GetPrimaryEntity(virtualCamera);
                AddHybridComponent(virtualCamera);
                AddHybridComponent(virtualCamera.GetComponent<Transform>());

                LoadCinemachineComponents(virtualCamera);
                if (virtualCamera.m_Follow != null)
                {
                    var followedEntity = TryGetPrimaryEntity(virtualCamera.m_Follow.gameObject);
                    if (followedEntity != Entity.Null)
                    {
                        Debug.Log("Follow " + followedEntity.Index);
                        DstEntityManager.AddComponentData(virtualCameraEntity, new Follow() { Entity = followedEntity });
                    }
                    AddHybridComponent(virtualCamera.m_Follow);
                }
                if (virtualCamera.m_LookAt != null)
                {
                    var lookAtEntity = TryGetPrimaryEntity(virtualCamera.m_LookAt.gameObject);
                    if (lookAtEntity != Entity.Null)
                    {
                        Debug.Log("Look At " + lookAtEntity.Index);
                        DstEntityManager.AddComponentData(virtualCameraEntity, new LookAt() { Entity = lookAtEntity });
                        AddHybridComponent(virtualCamera.m_LookAt);
                    }

                }

            });
        }

        private void LoadCinemachineComponents(CinemachineVirtualCamera virtualCamera)
        {

            foreach (Transform child in virtualCamera.transform)
            {
                var pipeline = child.GetComponent<CinemachinePipeline>();
                if (pipeline != null)
                {
                    var componentEntity = GetPrimaryEntity(pipeline);
                    AddHybridComponent(pipeline);
                    AddHybridComponent(pipeline.GetComponent<Transform>());
                    CinemachineComponentBase[] components = child.GetComponents<CinemachineComponentBase>();
                    DstEntityManager.AddComponentData(componentEntity, new RebuildHierachy { LocalToWorld = virtualCamera.transform.localToWorldMatrix });
                    foreach (CinemachineComponentBase c in components)
                    {

                        AddHybridComponent(c);
                        AddHybridComponent(c.GetComponent<Transform>());
                    }
                    DeclareLinkedEntityGroup(child.gameObject);
                }
            }
            DeclareLinkedEntityGroup(virtualCamera.gameObject);

        }
    }
    [UpdateBefore(typeof(CinemachineVirtualCameraHybridSystem))]
    public class CinemachineVirtualCameraSpawnerFollow : SystemBase
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
    public class CinemachineVirtualCameraHybridSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
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
           .ForEach((Entity e, CinemachineVirtualCamera camera, in LookAt target) =>
           {
               var transform = GetTransform(target.Entity, EntityManager);
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
            if (em.HasComponent<Transform>(entity))
            {
                return em.GetComponentObject<Transform>(entity);
            }

            return null;
        }
    }
}

[UpdateBefore(typeof(CinemachineVirtualCameraHybridSystem))]
public class RebuildGameObjectHierachySystem : SystemBase
{
    EntityCommandBufferSystem entityCommandBufferSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {


        var em = EntityManager;
        var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        Entities.ForEach((Entity e, Transform t, in Parent parent, in RebuildHierachy rebuildHierachy) =>
        {
            if (em.HasComponent<Transform>(parent.Value))
            {

                var parentTransform = em.GetComponentObject<Transform>(parent.Value);
                t.parent = parentTransform;
                t.transform.position = rebuildHierachy.LocalToWorld.c3.xyz;
                /*          CinemachineVirtualCamera.CreatePipelineOverride(parentTransform.GetComponent<CinemachineVirtualCamera>(), CinemachineVirtualCamera.PipelineName, t.GetComponents<CinemachineComponentBase>());
          */
            }
        }).WithoutBurst().Run();

    }
}