using UnityEngine;
using Unity.Entities;


namespace RPG.Core
{
    public class DynamicBufferTriggerEventAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddBuffer<StatefulTriggerEvent>(entity);
        }
    }

}
