using RPG.Combat;
using Unity.Entities;
using UnityEngine;

namespace RPG.Gameplay
{
    public struct ProjectileHitAudio : IComponentData
    {
        public Entity Entity;
    }
    public struct ProjectileLaunchAudio : IComponentData
    {
        public Entity Entity;
    }
    public class ProjectileAudioAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public AudioSource ProjectileHit;

        public AudioSource ProjectileLaunch;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (ProjectileHit != null)
            {
                var audioEntity = DeclareAudioSource(ProjectileHit, conversionSystem);
                dstManager.AddComponentData(entity, new ProjectileHitAudio { Entity = audioEntity });
            }
            if (ProjectileLaunch != null)
            {
                var audioEntity = DeclareAudioSource(ProjectileLaunch, conversionSystem);
                dstManager.AddComponentData(entity, new ProjectileLaunchAudio { Entity = audioEntity });
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
    public class ProjectileAudioSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
           .WithAll<ProjectileHitted>()
           .ForEach((Entity e, in Projectile p) =>
           {
               if (HasComponent<ProjectileHitAudio>(e))
               {
                   var weaponHitAudio = EntityManager.GetComponentData<ProjectileHitAudio>(e);
                   var audioSource = EntityManager.GetComponentObject<AudioSource>(weaponHitAudio.Entity);
               }
           }).WithoutBurst().Run();

            Entities
            .WithAll<ProjectileShooted>()
            .ForEach((Entity e, in Projectile projectile) =>
            {
                if (HasComponent<ProjectileLaunchAudio>(e))
                {
                    var weaponHitAudio = EntityManager.GetComponentData<ProjectileLaunchAudio>(e);
                    var audioSource = EntityManager.GetComponentObject<AudioSource>(weaponHitAudio.Entity);
                    audioSource.Play();
                }

            }).WithoutBurst().Run();
        }
    }
}
