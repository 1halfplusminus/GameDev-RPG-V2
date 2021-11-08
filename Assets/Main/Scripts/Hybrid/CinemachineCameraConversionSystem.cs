using Cinemachine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using RPG.Core;


namespace RPG.Hybrid
{
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

                var virtualCameraEntity = DstEntityManager.CreateEntity();
                DstEntityManager.AddComponentObject(virtualCameraEntity, virtualCamera);
                if (virtualCamera.m_Follow != null)
                {
                    var followedEntity = TryGetPrimaryEntity(virtualCamera.m_Follow.gameObject);
                    if (followedEntity != Entity.Null)
                    {
                        Debug.Log("Follow " + followedEntity.Index);
                        DstEntityManager.AddComponentData(followedEntity, new FollowedBy() { Entity = virtualCameraEntity });
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
                        DstEntityManager.AddComponentData(lookAtEntity, new LookAtBy() { Entity = virtualCameraEntity });
                        DstEntityManager.AddComponentData(virtualCameraEntity, new LookAt() { Entity = lookAtEntity });
                        AddHybridComponent(virtualCamera.m_LookAt);
                    }

                }

            });
        }
    }

    public class CinemachineVirtualCameraHybriSystem : SystemBase
    {

        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()

            .ForEach((CinemachineVirtualCamera camera, in Follow target) =>
            {
                var transform = GetTransform(target.Entity, EntityManager);
                if (transform != null)
                {
                    camera.m_Follow = transform;
                }
            }).Run();
            Entities
           .WithoutBurst()

           .ForEach((CinemachineVirtualCamera camera, in LookAt target) =>
           {
               var transform = GetTransform(target.Entity, EntityManager);
               if (transform != null)
               {
                   camera.m_LookAt = transform;
               }
           }).Run();

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
