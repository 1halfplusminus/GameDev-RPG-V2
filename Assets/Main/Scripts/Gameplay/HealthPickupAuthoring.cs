using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace RPG.Gameplay
{
    public struct HealthPickup : IComponentData { }
    public struct HealingAudio : IComponentData
    {

        public Entity Entity;
    }
    public class HealthPickupAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public AudioSource HealthAudioSource;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<HealthPickup>(entity);
            var audioEntity = DeclareAudioSource(HealthAudioSource, conversionSystem);
            if (HealthAudioSource != null)
            {
                dstManager.AddComponentData(entity, new HealingAudio { Entity = audioEntity });
            }
        }

        private Entity DeclareAudioSource(AudioSource audioSource, GameObjectConversionSystem conversionSystem)
        {
            return AudioConversionUtilities.DeclareAudioSource(audioSource,conversionSystem);
        }
    }

}
