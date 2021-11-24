
using RPG.Hybrid;
using Unity.Entities;
using Unity.Transforms;

namespace RPG.Core
{
    [UpdateInGroup(typeof(CoreSystemGroup))]
    public class CameraFollowSystem : SystemBase
    {
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
        }
        protected override void OnUpdate()
        {
            Entities
               .WithoutBurst()
               .WithStructuralChanges()
               .WithAll<FollowedByCamera>()
               .ForEach((Entity target) =>
               {
                   var position = EntityManager.GetComponentData<LocalToWorld>(target);
                   var thirdPersonCamera = GetSingletonEntity<ThirdPersonCamera>();
                   EntityManager.AddComponentData(thirdPersonCamera, new Follow { Entity = target });
                   EntityManager.AddComponentData(thirdPersonCamera, new LookAt { Entity = target });
               }).Run();

        }
    }
}
