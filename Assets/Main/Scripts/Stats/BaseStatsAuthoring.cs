
namespace RPG.Stats
{
    using RPG.Core;
    using Unity.Entities;
    using UnityEngine;

    public class BaseStatsAuthoring : MonoBehaviour
    {
        [Range(1, 99)]
        [SerializeField] public int StartLevel = 1;
        [SerializeField] public CharacterClass CharacterClass;

        [SerializeField] public ProgressionAsset Progression;

    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class BaseStatsDeclareReferencedObjectsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((BaseStatsAuthoring baseStatsAuthoring) =>
            {
                DeclareReferencedAsset(baseStatsAuthoring.Progression);
                DeclareAssetDependency(baseStatsAuthoring.gameObject, baseStatsAuthoring.Progression);
            });
        }
    }
    [UpdateAfter(typeof(HealthConversionSystem))]
    [UpdateAfter(typeof(ProgressionConversionSystem))]
    public class BaseStatsConversionSystem : GameObjectConversionSystem
    {
        ProgressionBlobAssetSystem progressionBlobAssetSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            progressionBlobAssetSystem = DstEntityManager.World.GetOrCreateSystem<ProgressionBlobAssetSystem>();
        }
        protected override void OnUpdate()
        {
            Entities.ForEach((BaseStatsAuthoring baseStatsAuthoring) =>
            {
                var progressionRef = progressionBlobAssetSystem.GetProgression(baseStatsAuthoring.CharacterClass);
                var entity = GetPrimaryEntity(baseStatsAuthoring);
                var health = progressionRef.Value.GetStat(Stats.Health, baseStatsAuthoring.StartLevel);
                var experience = progressionRef.Value.GetStat(Stats.RewardedExperiencePoint, baseStatsAuthoring.StartLevel);
                DstEntityManager.AddComponentData(entity, new BaseStats
                {
                    CharacterClass = baseStatsAuthoring.CharacterClass,
                    Level = baseStatsAuthoring.StartLevel,
                    ProgressionAsset = progressionRef
                });
                DstEntityManager.AddComponentData(entity, new Health
                {
                    Value = health,
                    MaxHealth = health
                });
                DstEntityManager.AddComponentData(entity, new GiveExperiencePoint { Value = experience });

            });

        }
    }
}