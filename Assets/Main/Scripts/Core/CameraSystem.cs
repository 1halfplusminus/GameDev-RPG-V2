

using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace RPG.Core
{
    public struct IsFollowingTarget : IComponentData { }

    public struct IsLookingAtTarget : IComponentData { }

    [UpdateInGroup(typeof(CoreSystemGroup))]
    [UpdateAfter(typeof(SceneLoadingSystem))]
    public partial class CameraFollowSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery cameraQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            cameraQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]{
                    ComponentType.ReadOnly<ThirdPersonCamera>(),

                 }
                // None = new ComponentType[]{
                //     ComponentType.ReadOnly<IsFollowingTarget>()
                // }
            });
            RequireSingletonForUpdate<ThirdPersonCamera>();
            RequireForUpdate(cameraQuery);
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var thirdPersonCamera = cameraQuery.GetSingletonEntity();
            Entities

            .WithAll<FollowedByCamera>()
            .ForEach((Entity target) =>
            {
                Debug.Log("Followed by Camera");
                cb.AddComponent(thirdPersonCamera, new Follow { Entity = target });
                cb.AddComponent(thirdPersonCamera, new LookAt { Entity = target });
                cb.AddComponent<Spawned>(thirdPersonCamera);
            }).Schedule();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
