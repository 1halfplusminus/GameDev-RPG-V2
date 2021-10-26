using RPG.Core;
using Unity.Entities;
using RPG.Mouvement;
using RPG.Combat;
using Unity.Transforms;

namespace RPG.Control
{

    [UpdateAfter(typeof(CombatSystemGroup))]
    public class ControlSystemGroup : ComponentSystemGroup
    {

    }
    [GenerateAuthoringComponent]
    public struct PlayerControlled : IComponentData { }




    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ConvertToEntitySystem))]
    public class PlayerControlledInitialisationSystem : SystemBase
    {
        BeginSimulationEntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = commandBufferSystem.CreateCommandBuffer();
            Entities
            .WithChangeFilter<PlayerControlled>()
            .WithAll<PlayerControlled>()
            .ForEach((Entity e, in LocalToWorld localToWorld) =>
            {
                commandBuffer.AddComponent<MouseClick>(e);
                commandBuffer.AddComponent<Fighter>(e);
                commandBuffer.AddBuffer<HittedByRaycast>(e);
                commandBuffer.AddComponent(e, new Raycast { Distance = 100000f });
                commandBuffer.AddComponent<MoveTo>(e, new MoveTo(localToWorld.Position));
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
