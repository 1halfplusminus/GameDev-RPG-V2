using RPG.Core;
using Unity.Entities;
using RPG.Mouvement;
using RPG.Combat;
using Unity.Transforms;
using Unity.Animation.Hybrid;

namespace RPG.Control
{

    [UpdateAfter(typeof(CombatSystemGroup))]
    public class ControlSystemGroup : ComponentSystemGroup
    {

    }

    public struct PlayerControlled : IComponentData { }



    public class PlayerControlledConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PlayerControlledAuthoring playerControlled, AnimationGraph animationGraph) =>
            {
                var entity = GetPrimaryEntity(playerControlled);
                DstEntityManager.AddComponent<PlayerControlled>(entity);
                DstEntityManager.AddComponentData(entity, new Raycast { Distance = playerControlled.RaycastDistance });
                DstEntityManager.AddComponent<MouseClick>(entity);
            });
        }
    }
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ConvertToEntitySystem))]
    public class PlayerControlledInitialisationSystem : SystemBase
    {
        EntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = commandBufferSystem.CreateCommandBuffer();
            var fighters = GetComponentDataFromEntity<Fighter>(true);
            Entities
            /*         .WithReadOnly(fighters) */
            .WithChangeFilter<PlayerControlled>()
            .WithAll<PlayerControlled>()
            .ForEach((Entity e, in LocalToWorld localToWorld) =>
            {
                commandBuffer.AddComponent<MoveTo>(e, new MoveTo(localToWorld.Position));
                /* if (fighters.HasComponent(e))
                {
                    commandBuffer.AddComponent<Fighter>(e, new Fighter { WeaponRange = 3.0f, AttackCooldown = 5.0f });
                }
                commandBuffer.AddComponent(e, new Raycast { Distance = 100000f });
                commandBuffer.AddComponent<MouseClick>(e);
                commandBuffer.AddBuffer<HittedByRaycast>(e);
                commandBuffer.AddComponent<LookAt>(e);
                commandBuffer.AddComponent<DeltaTime>(e);
                commandBuffer.AddComponent<CharacterAnimation>(e); */
            }).Schedule();
            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class RaycastOnMouseClick : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
        }
        protected override void OnUpdate()
        {
            Entities
            .WithAll<PlayerControlled>()
            .WithChangeFilter<MouseClick>()
            .ForEach((ref Raycast cast, in MouseClick mouseClick) =>
            {
                cast.Completed = false;
                cast.Ray = mouseClick.Ray;
            }).ScheduleParallel();
        }
    }

    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class NoInteractionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithNone<WorldClick>()
            .WithAny<PlayerControlled>()
            .ForEach((Fighter f) =>
            {
                if (f.Target == Entity.Null)
                {
                    UnityEngine.Debug.Log("No interaction");
                }
            }).ScheduleParallel();
        }
    }
}
