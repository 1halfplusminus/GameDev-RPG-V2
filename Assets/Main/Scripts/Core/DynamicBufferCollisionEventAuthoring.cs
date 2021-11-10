
using Unity.Entities;
using UnityEngine;

namespace RPG.Core
{

    public class DynamicBufferCollisionEventAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [Tooltip("If selected, the details will be calculated in collision event dynamic buffer of this entity")]
        public bool CalculateDetails = false;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var dynamicBufferTag = new CollisionEventBuffer
            {
                CalculateDetails = CalculateDetails ? 1 : 0
            };

            dstManager.AddComponentData(entity, dynamicBufferTag);
            dstManager.AddBuffer<StatefulCollisionEvent>(entity);
        }
    }

}
