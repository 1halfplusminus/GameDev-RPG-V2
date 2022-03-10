
using RPG.Hybrid;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace RPG.Core
{

    [UpdateInGroup(typeof(CoreSystemGroup))]
    public class CameraFollowSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireSingletonForUpdate<ThirdPersonCamera>();
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]{
                    ComponentType.ReadOnly<ThirdPersonCamera>(),
                },
                None = new ComponentType[]{
                    ComponentType.ReadOnly<IsFollowingTarget>()
                }
            }));
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var thirdPersonCamera = GetSingletonEntity<ThirdPersonCamera>();
            Entities
            .WithNone<Spawned>()
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
