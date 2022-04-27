using Cinemachine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace RPG.Core
{

    [UpdateInGroup(typeof(CoreSystemGroup))]
    public partial class LookAtSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var localToWorlds = GetComponentDataFromEntity<LocalToWorld>(true);
            Entities
            .WithNone<IsDeadTag>()
            .WithReadOnly(localToWorlds)
            .WithChangeFilter<LookAt, LocalToWorld>()
            .ForEach((ref Rotation rotation, in LookAt lookAt, in LocalToWorld localToWorld) =>
            {
                if (localToWorlds.HasComponent(lookAt.Entity) == true)
                {
                    var targetLocalToWorld = localToWorlds[lookAt.Entity];
                    float3 heading = targetLocalToWorld.Position - localToWorld.Position;
                    heading.y = 0f;
                    rotation.Value = quaternion.LookRotation(heading, math.up());
                }
            }).ScheduleParallel();
        }
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    public partial class CameraFacingSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            RequireSingletonForUpdate<CinemachineBrain>();
        }
        protected override void OnUpdate()
        {
            if (HasSingleton<CinemachineBrain>())
            {
                var cameraEntity = GetSingletonEntity<CinemachineBrain>();
                var cb = entityCommandBufferSystem.CreateCommandBuffer();
                var cbp = cb.AsParallelWriter();
                var camera = Camera.main;
                var cameraPosition = (float3)camera.transform.position;
                var cameraForward = (float3)camera.transform.forward;
                var cameraUp = (float3)camera.transform.up;
                var cameraRotation = (quaternion)camera.transform.rotation;
                var cameraRight = (float3)camera.transform.right;
                Entities.ForEach((int entityInQueryIndex, Entity e, ref Rotation rotation, ref FaceCamera faceCamera, ref Translation ts) =>
                {
                    if (HasComponent<Parent>(e))
                    {
                        var parent = GetComponent<Parent>(e);
                        cbp.RemoveComponent<Parent>(entityInQueryIndex, e);
                        cbp.RemoveComponent<LocalToParent>(entityInQueryIndex, e);
                        faceCamera.Parent = parent.Value;
                    }
                    rotation.Value = cameraRotation;
                    if (faceCamera.Parent != Entity.Null)
                    {
                        ts.Value = GetComponent<LocalToWorld>(faceCamera.Parent).Position + faceCamera.Offset;
                    }
                }).ScheduleParallel();
                entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            }

        }
    }
}

