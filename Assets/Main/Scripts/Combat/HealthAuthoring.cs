using System;
using Unity.Entities;
using UnityEngine;


namespace RPG.Core
{
    [Serializable]
    public struct Health : IComponentData
    {
        public float Value;
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
                DstEntityManager.AddComponentData(entity, new Health { Value = healthAuthoring.Value });
            });
        }
    }
}
