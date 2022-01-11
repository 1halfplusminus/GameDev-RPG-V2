using RPG.Combat;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace RPG.Gameplay
{
    public struct TakeHitAudio : IComponentData
    {
        public Entity Entity;
    }
    public class DieAudio : IComponentData
    {
        public Entity Entity;
    }

    public class CharacterAudioAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {

        public AudioSource TakeHit;

        public AudioSource Die;


        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (TakeHit != null)
            {
                var audioEntity = DeclareAudioSource(TakeHit, conversionSystem);
                dstManager.AddComponentData(entity, new TakeHitAudio { Entity = audioEntity });
            }
            if (Die != null)
            {
                var audioEntity = DeclareAudioSource(Die, conversionSystem);
                dstManager.AddComponentData(entity, new DieAudio { Entity = audioEntity });
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
    public class CharacterAudioSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithAll<WasHitted>()
            .ForEach((in TakeHitAudio hitEffectAudio, in LocalToWorld localToWorld) =>
            {
                var audioSource = EntityManager.GetComponentObject<AudioSource>(hitEffectAudio.Entity);
                audioSource.Play();
            }).WithoutBurst().Run();

            Entities
           .WithAll<Died>()
           .ForEach((in DieAudio dieAudio) =>
           {
               var audioSource = EntityManager.GetComponentObject<AudioSource>(dieAudio.Entity);
               audioSource.Play();
           }).WithoutBurst().Run();

            Entities
           .WithNone<IsProjectile>()
           .ForEach((Entity e, in Hit hit) =>
           {
               if (hit.Damage > 0 && HasComponent<WeaponHitAudio>(hit.Hitter))
               {
                   var weaponHitAudio = EntityManager.GetComponentData<WeaponHitAudio>(hit.Hitter);
                   var audioSource = EntityManager.GetComponentObject<AudioSource>(weaponHitAudio.Entity);
                   audioSource.Play();
               }
           }).WithoutBurst().Run();

        }
    }
}
