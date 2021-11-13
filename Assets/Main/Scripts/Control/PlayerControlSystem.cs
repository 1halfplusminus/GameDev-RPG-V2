using Unity.Entities;
using RPG.Core;
using RPG.Mouvement;
using RPG.Combat;

namespace RPG.Control
{
    public struct DisabledControl : IComponentData { };
    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class PlayersMoveSystem : SystemBase
    {
        EntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithAll<PlayerControlled>()
            .WithNone<DisabledControl>()
            .ForEach((Entity player, int entityInQueryIndex, ref MoveTo moveTo, in MouseClick mouseClick, in WorldClick worldClick) =>
            {
                if (mouseClick.CapturedThisFrame)
                {
                    moveTo.Stopped = false;
                    moveTo.Position = worldClick.WorldPosition;
                }

            }).ScheduleParallel();

            Entities
            .WithNone<DisabledControl>()
            .WithAll<PlayerControlled>()
            .ForEach((Entity player, ref Fighter fighter, in MouseClick mouseClick) =>
            {

                if (mouseClick.CapturedThisFrame)
                {
                    if (fighter.TargetFoundThisFrame == 0)
                    {
                        fighter.Target = Entity.Null;
                    }
                    if (fighter.Target == Entity.Null)
                    {

                        fighter.MoveTowardTarget = false;
                    }
                    else
                    {
                        fighter.MoveTowardTarget = true;
                    }
                }

            }).ScheduleParallel();
            // Look at fighter target if exists
            Entities
            .WithNone<DisabledControl>()
            .WithAll<PlayerControlled>()
            .ForEach((ref LookAt lookAt, in Fighter fighter, in MouseClick mouseClick) =>
            {
                if (mouseClick.CapturedThisFrame)
                {
                    lookAt.Entity = fighter.Target;
                }

            }).ScheduleParallel();
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
    [DisableAutoCreation]

    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class NoInteractionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithNone<DisabledControl>()
            .WithNone<WorldClick>()
            .WithAny<PlayerControlled>()
            .ForEach((in Fighter f) =>
            {
                if (f.Target == Entity.Null)
                {

                }
            }).ScheduleParallel();
        }
    }
}

