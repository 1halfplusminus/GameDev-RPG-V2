using Unity.Entities;
using Unity.Rendering;

namespace RPG.Core
{
    public struct DeltaTime : IComponentData
    {
        public float Value;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]

    public partial class DeltaTimeSystem : SystemBase
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