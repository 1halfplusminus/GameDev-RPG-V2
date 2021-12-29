using System;
using Unity.Entities;
using UnityEngine;

namespace RPG.Stats
{


    [Serializable]
    public struct ExperiencePoint : IComponentData
    {
        public float Value;

        public int GetLevel(BlobAssetReference<Progression> progressionAsset)
        {
            var index = (int)Stats.ExperiencePointToLevelUp;
            ref var experiencePointToLevels = ref progressionAsset.Value.Stats[index];
            for (int i = 0; i < experiencePointToLevels.Length; i++)
            {
                if (experiencePointToLevels[i] > Value)
                {
                    return i + 1;
                }
            }
            return experiencePointToLevels.Length;
        }
    }

    public class ExperiencePointAuthoring : MonoBehaviour
    {
        public float Value;
    }

    [UpdateAfter(typeof(BaseStatsConversionSystem))]
    public class ExperiencePointConversionSystem : GameObjectConversionSystem
    {
        ProgressionBlobAssetSystem progressionBlobAssetSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            progressionBlobAssetSystem = DstEntityManager.World.GetOrCreateSystem<ProgressionBlobAssetSystem>();
        }
        protected override void OnUpdate()
        {
            Entities.ForEach((BaseStatsAuthoring baseStatsAuthoring, ExperiencePointAuthoring experiencePointAuthoring) =>
            {
                var progressionRef = progressionBlobAssetSystem.GetProgression(baseStatsAuthoring.CharacterClass);
                var entity = GetPrimaryEntity(experiencePointAuthoring);
                DstEntityManager.AddComponentData(entity, new ExperiencePoint
                {

                    // ProgressionAsset = progressionRef
                });
            });

        }
    }
}