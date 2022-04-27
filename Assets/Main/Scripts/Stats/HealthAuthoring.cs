using System;
using RPG.Stats;
using Unity.Entities;
using UnityEngine;


namespace RPG.Core
{
    [Serializable]
    public struct Health : IComponentData
    {
        public float MaxHealth;
        public float Value;

        public float GetRatio(int level, BlobAssetReference<Progression> progressionAsset)
        {
            return Value / progressionAsset.Value.GetStat(Stats.Stats.Health, level);
        }
    }


    public class HealthAuthoring : MonoBehaviour
    {
        [Min(0.0f)]
        public float Value;
    }

    public class HealthConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((HealthAuthoring healthAuthoring) =>
            {
                var entity = GetPrimaryEntity(healthAuthoring);
                DstEntityManager.AddComponentData(entity, new Health { Value = healthAuthoring.Value, MaxHealth = healthAuthoring.Value });
            });
        }
    }
}
