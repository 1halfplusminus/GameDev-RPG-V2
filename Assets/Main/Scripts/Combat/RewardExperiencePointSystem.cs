using RPG.Core;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using RPG.Stats;
using RPG.Combat;

namespace RPG.Stats {

    [UpdateInGroup(typeof(ResourceSystemGroup))]
    public partial class RewardExperiencePointSystem : SystemBase
    {
        private EntityCommandBufferSystem entityCommandBufferSystem;
        private EntityQuery leveledUpEntityQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            leveledUpEntityQuery = GetEntityQuery(ComponentType.ReadOnly<LeveledUp>());
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            EntityCommandBuffer cb = entityCommandBufferSystem.CreateCommandBuffer();
            EntityCommandBuffer.ParallelWriter cbp = cb.AsParallelWriter();
            cb.RemoveComponentForEntityQuery<LeveledUp>(leveledUpEntityQuery);

            Entities
            .WithAll<IsDeadTag>()
            .WithNone<ExperiencePointRewarded>()
            .ForEach((int entityInQueryIndex, Entity e, in DynamicBuffer<WasHitteds> wasHitteds, in GiveExperiencePoint experiencePoint) =>
            {
                for (int i = 0; i < wasHitteds.Length; i++)
                {
                    WasHitteds wasHitted = wasHitteds[i];
                    Entity hitter = wasHitted.Hitter;
                    if (HasComponent<ExperiencePoint>(hitter))
                    {
                        Debug.Log($"Reward {hitter.Index} with {experiencePoint.Value}");
                        ExperiencePoint exp = GetComponent<ExperiencePoint>(hitter);
                        exp.Value += experiencePoint.Value;
                        cbp.AddComponent(entityInQueryIndex, hitter, exp);
                        if (HasComponent<BaseStats>(hitter))
                        {
                            BaseStats baseStats = GetComponent<BaseStats>(hitter);
                            int newLevel = exp.GetLevel(baseStats.ProgressionAsset);
                            if (newLevel != baseStats.Level)
                            {
                                Debug.Log($"Entity {hitter.Index} Level up from level: {baseStats.Level} to level: {newLevel}");
                                baseStats.Level = newLevel;
                                cbp.AddComponent(entityInQueryIndex, hitter, baseStats);
                                cbp.AddComponent<LeveledUp>(entityInQueryIndex, hitter);
                            }
                        }
                        break;
                    }
                }
                cbp.AddComponent<ExperiencePointRewarded>(entityInQueryIndex, e);
            }).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}