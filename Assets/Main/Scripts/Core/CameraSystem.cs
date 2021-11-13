
using Unity.Entities;
using Unity.Transforms;

namespace RPG.Core
{
    [UpdateInGroup(typeof(CoreSystemGroup))]
    public class CameraFollowSystem : SystemBase
    {

        protected override void OnUpdate()
        {
            if (HasSingleton<ThirdPersonCamera>())
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
}
