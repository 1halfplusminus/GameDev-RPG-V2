
namespace RPG.Stats
{
    using RPG.Combat;
    using RPG.Core;
    using Unity.Entities;
    using UnityEngine;
    public struct GiveExperiencePoint : IComponentData
    {
        public float Value;
    }
    public struct BaseStats : IComponentData
    {
        public int Level;
        public CharacterClass CharacterClass;
    }
    public enum CharacterClass : byte
    {
        Novice = 0x1,
        Mage = 0x2,
        Archer = 0x3,
        Warrior = 0x4
    }
    public enum Stats : int
    {
        RewardedExperiencePoint = 0,
        Health = 1
    }

    public struct ExperiencePointRewarded : IComponentData
    {

    }
    [UpdateInGroup(typeof(ResourceSystemGroup))]
    public class RewardExperiencePointSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cbp = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.WithAll<IsDeadTag>()
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
                        break;
                    }
                }
                cbp.AddComponent<ExperiencePointRewarded>(entityInQueryIndex, e);
            }).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}