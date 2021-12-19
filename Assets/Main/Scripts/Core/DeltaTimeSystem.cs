using Unity.Entities;

namespace RPG.Core
{
    public struct DeltaTime : IComponentData
    {
        public float Value;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]

    public class UpdateAnimationDeltaTime : SystemBase
    {
        protected override void OnUpdate()
        {
            var worldDetaTime = World.Time.DeltaTime;
            Entities.ForEach((Entity Entity, ref DeltaTime dt) =>
            {
                dt.Value = worldDetaTime;
            }).ScheduleParallel();
        }
    }


}