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
    public class HideSystem : SystemBase
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
            .WithAll<HideForSecond>().ForEach((int entityInQueryIndex, Entity e, ref HideForSecond hideForSecond, in DeltaTime dt) =>
            {
                Debug.Log($"{e.Index} hide for {hideForSecond.Time}");
                hideForSecond.Time -= dt.Value;
                if (hideForSecond.Time < 0)
                {
                    Debug.Log($"Unhide {e.Index}");
                    cbp.AddComponent<UnHide>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();
            cb.RemoveComponent<UnHide>(cleanupUnHideQuery);
            cb.RemoveComponent<Hidden>(unHideQuery);
            cb.RemoveComponent<HideForSecond>(unHideQuery);
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

}