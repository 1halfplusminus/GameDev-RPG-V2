using System;
using RPG.Combat;
using RPG.Core;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;

namespace RPG.Stats
{
    public struct FixedListStat
    {
        public FixedListFloat32 Values;
        public void Add(Stats stat, float value)
        {
            var currentStat = GetStat(stat);
            SetStat(stat, currentStat + value);
        }
        public float GetStat(Stats stat)
        {
            return GetStat((int)stat);
        }
        public float GetStat(int stat)
        {
            return stat < Values.Length ? Values[stat] : 0;
        }
        public void Resize(int size)
        {
            if (size > Values.Length)
            {
                // Values.Capacity = size;
                for (int i = Values.Length; i < size; i++)
                {
                    Values.Add(0f);
                    // Debug.Log("Resize value");
                }
            }
        }
        public void SetStat(int key, float value)
        {
            Resize(key + 1);
            Values[key] = value;
        }
        public void SetStat(Stats stat, float value)
        {
            SetStat((int)stat, value);
        }

    }

    public struct CalculedStat : IComponentData
    {
        public FixedListStat Stats;


        public float GetStat(Stats stat)
        {
            return Stats.GetStat(stat);
        }
        public float GetStat(int stat)
        {
            return Stats.GetStat(stat);
        }
    }
    public struct AdditiveStatsModifier : IComponentData
    {
        public FixedListStat Stats;

        public float GetStat(Stats stat)
        {
            return Stats.GetStat(stat);
        }
    }
    [Serializable]
    public struct StatsModifier : IBufferElementData
    {

        public Stats Stats;
        public float Value;

        public Entity Entity;
    }
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
    public class BaseStatSystem : SystemBase
    {
        EntityQuery modifiersQuery;
        protected override void OnUpdate()
        {
            var statsCount = Enum.GetNames(typeof(Stats)).Length;
            var modifiersByEntities = new NativeMultiHashMap<Entity, StatsModifier>(modifiersQuery.CalculateEntityCount(), Allocator.TempJob);
            var pw = modifiersByEntities.AsParallelWriter();
            Entities
            .WithStoreEntityQueryInField(ref modifiersQuery)
            .ForEach((Entity e, in DynamicBuffer<StatsModifier> modifiers) =>
            {
                for (int i = 0; i < modifiers.Length; i++)
                {
                    var modifier = modifiers[i];
                    pw.Add(modifier.Entity, modifier);
                }
            }).ScheduleParallel();

            Entities
            .WithReadOnly(modifiersByEntities)
            .WithDisposeOnCompletion(modifiersByEntities)
            .ForEach((Entity e, ref AdditiveStatsModifier modifier) =>
            {
                modifier.Stats.Resize(statsCount);
                for (int i = 0; i < statsCount; i++)
                {
                    modifier.Stats.SetStat(i, 0);
                }
                if (modifiersByEntities.ContainsKey(e))
                {
                    foreach (var statModifier in modifiersByEntities.GetValuesForKey(e))
                    {
                        // Debug.Log($"{e.Index} Add Found modifier {statModifier.Value}");
                        modifier.Stats.Add(statModifier.Stats, statModifier.Value);
                    }
                }

            }).ScheduleParallel();


            Entities
            .ForEach((Entity e, ref CalculedStat calculedStat, in AdditiveStatsModifier modifier, in BaseStats baseStats) =>
            {
                calculedStat.Stats.Resize(statsCount);
                modifier.Stats.Resize(statsCount);
                for (int i = 0; i < statsCount; i++)
                {
                    var baseStat = baseStats.ProgressionAsset.Value.GetStat(i, baseStats.Level);
                    var newStat = baseStat + modifier.Stats.GetStat(i);
                    calculedStat.Stats.SetStat(i, newStat);
                    // Debug.Log($"Caculed stat for {baseStat} {modifier.Stats.GetStat(i)}");
                }
            }).ScheduleParallel();
        }
    }
    [UpdateInGroup(typeof(ResourceSystemGroup))]
    public class RewardExperiencePointSystem : SystemBase
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
            .ForEach((int entityInQueryIndex, Entity e, in DynamicBuffer<WasHitted> wasHitteds, in GiveExperiencePoint experiencePoint) =>
            {
                for (int i = 0; i < wasHitteds.Length; i++)
                {
                    WasHitted wasHitted = wasHitteds[i];
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