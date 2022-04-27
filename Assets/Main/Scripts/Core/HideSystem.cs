using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RPG.Core
{

    public struct UnHide : IComponentData
    {

    }
    public struct Hidden : IComponentData
    {

    }
    public struct HideForSecond : IComponentData
    {
        public float Time;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    public partial class HideSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery unHideQuery;

        EntityQuery cleanupUnHideQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            unHideQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(UnHide)
                 }
            });
            cleanupUnHideQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(UnHide)
                 },
                None = new ComponentType[] {
                    typeof(Hidden)
                 }
            });
        }
        protected override void OnUpdate()
        {

            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            Entities
            .WithNone<Hidden>()
            .WithAll<HideForSecond>().ForEach((int entityInQueryIndex, Entity e) =>
            {
                cbp.AddComponent<Hidden>(entityInQueryIndex, e);
                cbp.AddComponent<DeltaTime>(entityInQueryIndex, e);
            }).ScheduleParallel();

            Entities
            .WithAll<Hidden>()
            .WithAll<HideForSecond>()
            .ForEach((int entityInQueryIndex, Entity e, ref HideForSecond hideForSecond, in DeltaTime dt) =>
            {
                hideForSecond.Time -= dt.Value;
                if (hideForSecond.Time < 0)
                {
                    cbp.AddComponent<UnHide>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();
            cb.RemoveComponentForEntityQuery<UnHide>(cleanupUnHideQuery);
            cb.RemoveComponentForEntityQuery<Hidden>(unHideQuery);
            cb.RemoveComponentForEntityQuery<HideForSecond>(unHideQuery);
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

}