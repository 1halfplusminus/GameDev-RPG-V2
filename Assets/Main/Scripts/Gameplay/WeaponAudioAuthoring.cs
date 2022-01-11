using RPG.Combat;
using Unity.Entities;
using UnityEngine;

namespace RPG.Gameplay
{
    public struct WeaponHitAudio : IComponentData
    {
        public Entity Entity;
    }
    public class WeaponAudioAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public AudioSource Hit;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (Hit != null)
            {
                var audioEntity = DeclareAudioSource(Hit, conversionSystem);
                dstManager.AddComponentData(entity, new WeaponHitAudio { Entity = audioEntity });
            }
        }
        private Entity DeclareAudioSource(AudioSource audioSource, GameObjectConversionSystem conversionSystem)
        {
            conversionSystem.DeclareAssetDependency(audioSource.gameObject, audioSource.clip);
            conversionSystem.AddHybridComponent(audioSource);
            return conversionSystem.GetPrimaryEntity(audioSource.gameObject);
        }
    }

    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public class WeaponAudioSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .ForEach((in Hit hit) =>
            {
                if (hit.Trigger != Entity.Null && HasComponent<WeaponHitAudio>(hit.Trigger))
                {
                    var weaponHitAudioSource = EntityManager.GetComponentData<WeaponHitAudio>(hit.Trigger);
                    var audioSource = EntityManager.GetComponentObject<AudioSource>(weaponHitAudioSource.Entity);
                    audioSource.Play();
                }
            }).WithoutBurst().Run();

        }
    }
}
