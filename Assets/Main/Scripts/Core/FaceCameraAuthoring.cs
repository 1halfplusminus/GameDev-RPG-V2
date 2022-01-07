using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RPG.Core
{

    public struct FaceCamera : IComponentData
    {
        public Entity Parent;
        public float3 Offset;
    }


    public class FaceCameraAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new FaceCamera { Offset = transform.localPosition });
        }
    }
}