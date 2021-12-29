
namespace RPG.Stats
{
    using System;
    using RPG.Combat;
    using RPG.Core;
    using Unity.Entities;
    using UnityEngine;

    public struct GiveExperiencePoint : IComponentData
    {
        public float Value;
    }

    [Serializable]
    public struct BaseStats : IComponentData
    {
        public int Level;
        public CharacterClass CharacterClass;

        [NonSerialized]

        public BlobAssetReference<Progression> ProgressionAsset;
    }

    public struct ExperiencePointRewarded : IComponentData
    {

    }

    public struct LeveledUp : IComponentData
    {

    }
    [UpdateInGroup(typeof(ResourceSystemGroup))]
    public class RewardExperiencePointSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery leveledUpEntityQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            leveledUpEntityQuery = GetEntityQuery(ComponentType.ReadOnly<LeveledUp>());
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            cb.RemoveComponentForEntityQuery<LeveledUp>(leveledUpEntityQuery);

            Entities
            .WithAll<IsDeadTag>()
            .WithNone<ExperiencePointRewarded>()
            .ForEach((int entityInQueryIndex, Entity e, in DynamicBuffer<WasHitted> wasHitteds, in GiveExperiencePoint experiencePoint) =>
            {
                for (int i = 0; i < wasHitteds.Length; i++)
                {
                    var wasHitted = wasHitteds[i];
                    var hitter = wasHitted.Hitter;
                    if (HasComponent<ExperiencePoint>(hitter))
                    {
                        Debug.Log($"Reward {hitter.Index} with {experiencePoint.Value}");
                        var exp = GetComponent<ExperiencePoint>(hitter);
                        exp.Value += experiencePoint.Value;
                        cbp.AddComponent(entityInQueryIndex, hitter, exp);
                        if (HasComponent<BaseStats>(hitter))
                        {
                            var baseStats = GetComponent<BaseStats>(hitter);
                            var newLevel = exp.GetLevel(baseStats.ProgressionAsset);
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