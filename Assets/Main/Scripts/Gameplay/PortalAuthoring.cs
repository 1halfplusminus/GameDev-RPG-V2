
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using RPG.Core;
using RPG.Control;

namespace RPG.Gameplay
{
    public class PortalAuthoring : MonoBehaviour
    {
        public Scene Scene;

        public int OtherScenePortalIndex;

        public int PortalIndex;
    }

    public class PortalConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PortalAuthoring portalAuthoring) =>
            {
                var entity = GetPrimaryEntity(portalAuthoring);
                DstEntityManager.AddComponentData(entity, new Portal { Index = portalAuthoring.PortalIndex });
            });
        }
    }
    public struct Portal : IComponentData
    {
        public int Index;
    }
    public struct CollidWithPlayer : IComponentData
    {
        public Entity Entity;
    }
    public class CollidWithPlayerSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBufferP = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var players = GetComponentDataFromEntity<PlayerControlled>(true);
            Entities.ForEach((int entityInQueryIndex, Entity e, DynamicBuffer<StatefulTriggerEvent> triggerEvents) =>
            {
                foreach (var triggerEvent in triggerEvents)
                {
                    var otherEntity = triggerEvent.GetOtherEntity(e);
                    if (players.HasComponent(otherEntity))
                    {
                        Debug.Log("Collid with player");
                        commandBufferP.AddComponent(entityInQueryIndex, e, new CollidWithPlayer { Entity = otherEntity });
                        break;
                    }
                }
            }).WithReadOnly(players).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

    public class PortalSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((in CollidWithPlayer collidWithPlayer, in Portal portal) =>
            {
                Debug.Log("Change scene");
            }).ScheduleParallel();
        }
    }
}
