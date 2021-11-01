using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
}