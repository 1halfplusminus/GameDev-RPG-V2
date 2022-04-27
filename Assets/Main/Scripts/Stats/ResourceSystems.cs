using System;
// using RPG.Combat;
using RPG.Core;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;

namespace RPG.Stats
{
    public struct FixedListStat
    {
        public FixedList32Bytes<float> Values;
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
    public struct PercentStatsModifier : IComponentData
    {
        public FixedListStat Stats;

        public float GetStat(Stats stat)
        {
            return Stats.GetStat(stat);
        }
    }
    public enum StatModifierType
    {
        Additive, Percent
    }
    [Serializable]
    public struct StatsModifier : IBufferElementData
    {

        public Stats Stats;
        public float Value;
        public StatModifierType Type;
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
    public partial class BaseStatSystem : SystemBase
    {
        EntityQuery modifiersQuery;
        protected override void OnUpdate()
        {
            var statsCount = Enum.GetNames(typeof(Stats)).Length;
            var modifiersByEntities = new NativeMultiHashMap<Entity, StatsModifier>(modifiersQuery.CalculateEntityCount() * 10, Allocator.TempJob);
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
            .ForEach((Entity e, ref AdditiveStatsModifier additiveModifier, ref PercentStatsModifier percentModifier) =>
            {
                additiveModifier.Stats.Resize(statsCount);
                for (int i = 0; i < statsCount; i++)
                {
                    additiveModifier.Stats.SetStat(i, 0);
                    percentModifier.Stats.SetStat(i, 0);
                }
                if (modifiersByEntities.ContainsKey(e))
                {
                    foreach (var statModifier in modifiersByEntities.GetValuesForKey(e))
                    {
                        if (statModifier.Type == StatModifierType.Additive)
                        {
                            additiveModifier.Stats.Add(statModifier.Stats, statModifier.Value);
                        }
                        if (statModifier.Type == StatModifierType.Percent)
                        {
                            percentModifier.Stats.Add(statModifier.Stats, statModifier.Value);
                        }
                    }
                }

            }).ScheduleParallel();


            Entities
            .ForEach((Entity e, ref CalculedStat calculedStat, in AdditiveStatsModifier additiveModifier, in PercentStatsModifier percentModifier, in BaseStats baseStats) =>
            {
                calculedStat.Stats.Resize(statsCount);
                additiveModifier.Stats.Resize(statsCount);
                for (int i = 0; i < statsCount; i++)
                {
                    var baseStat = baseStats.ProgressionAsset.Value.GetStat(i, baseStats.Level);
                    var newStat = (baseStat + additiveModifier.Stats.GetStat(i)) * (1f + (percentModifier.Stats.GetStat(i) / 100f));
                    calculedStat.Stats.SetStat(i, newStat);
                }
            }).ScheduleParallel();
        }
    }

}