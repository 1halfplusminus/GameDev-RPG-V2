
using RPG.Core;
using Unity.Entities;
using UnityEngine;
using RPG.Hybrid;
namespace RPG.Gameplay
{
    public struct PlayingAudio : IComponentData
    {
    }
    public struct PlayAudio : IComponentData
    {
    }
    public struct StopAudio : IComponentData
    {
    }
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial class AudioSourceSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            Entities
            .WithNone<PlayingAudio>()
            .WithAll<PlayAudio>().ForEach((Entity e, AudioSource audioSource) =>
            {
                audioSource.Play();
                cb.AddComponent<PlayingAudio>(e);
                cb.RemoveComponent<PlayAudio>(e);
            })
            .WithoutBurst()
            .Run();

            Entities
            .WithAll<PlayingAudio, StopAudio>().ForEach((Entity e, AudioSource audioSource) =>
            {
                audioSource.Stop();
                cb.RemoveComponent<StopAudio>(e);
                cb.RemoveComponent<PlayingAudio>(e);
            }).WithoutBurst().Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    public struct TriggerAudioOnCollion : IComponentData
    {
        public Entity AudioSource;
    }

    public class TriggerAudioOnCollisionAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField]
        AudioSource AudioSource;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {

            var audioSourceEntity = conversionSystem.GetPrimaryEntity(AudioSource.gameObject);
            dstManager.AddComponentData(entity, new TriggerAudioOnCollion { AudioSource = audioSourceEntity });
        }
    }
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial class TriggerAudioSourceOnCollisionSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cpb = cb.AsParallelWriter();
            Entities
            .ForEach((int entityInQueryIndex, Entity e, in TriggerAudioOnCollion trigger, in DynamicBuffer<StatefulTriggerEvent> statefulTriggerEvent) =>
            {
                for (int i = 0; i < statefulTriggerEvent.Length; i++)
                {
                    var triggerEvent = statefulTriggerEvent[i];
                    if (triggerEvent.State == EventOverlapState.Enter)
                    {
                        Debug.Log($"Trigger audio on collision with {e.Index}");
                        cpb.AddComponent<PlayAudio>(entityInQueryIndex, trigger.AudioSource);
                    }
                    if (triggerEvent.State == EventOverlapState.Exit)
                    {
                        Debug.Log($"Stop audio on collision with {e.Index}");
                        cpb.AddComponent<StopAudio>(entityInQueryIndex, trigger.AudioSource);
                    }
                }
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}