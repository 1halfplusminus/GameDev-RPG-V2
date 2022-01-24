using Cinemachine;
using RPG.Hybrid;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace RPG.Core
{

    [UpdateInGroup(typeof(CoreSystemGroup))]
    public class LookAtSystem : SystemBase
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
    public class CameraFacingSystem : SystemBase
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

// FIXME: TO DELETE
// var direction = math.normalize(localToWorld.Position - cameraPosition);
// if (math.dot(localToWorld.Right, direction) > 0.98f)
// {
//     rotation.Value = quaternion.LookRotation(direction, math.up());
// }
// forward.z = 0;

// scale.Value.x = -1;
// var heading = -cameraForward;
// heading.y = 0;
// textMesh.transform.rotation = Quaternion.LookRotation(heading, textMesh.transform.up - camera.transform.up);

// rotation.Value = textMesh.transform.rotation;

// var heading = (cameraPosition + cameraForward);
// var up = localToWorld.Up - cameraUp;
// heading.y = 0;
// var result = quaternion.LookRotationSafe(heading, cameraUp);
// var angleBetweenForward = math.dot(localToWorld.Forward, cameraForward);
// var dif = math.mul(rotation.Value, math.inverse(cameraRotation));
// Debug.Log($"Forward my forward {localToWorld.Forward} heading:{heading} cameraForward: {cameraForward}");
// Debug.Log($"Angle between {math.degrees(angleBetweenForward)}");
// Debug.Log($"Look rotation: x:{math.degrees(result.value.x)} y:{math.degrees(result.value.y)} z:{math.degrees(result.value.z)}");
// Debug.Log($"rotation difference x:{dif.value.x} y:{dif.value.y}  z:{dif.value.z} w:{dif.value.z} w: {dif.value.w}");
// result.value.x = 0;
// rotation.Value = result;
// rotation.Value.value.w = rotation.Value.value.w;
// rotation.Value.value.y = math.radians(90f);