using Unity.Entities;
using RPG.Core;
using RPG.Mouvement;
using RPG.Combat;
using UnityEngine;

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
            .WithAll<DisabledControl>()
            .ForEach((ref MoveTo moveTo) =>
            {
                moveTo.Stopped = true;

            }).ScheduleParallel();
            Entities
            .WithAll<PlayerControlled>()
            .WithNone<DisabledControl>()
            .ForEach((Entity player, int entityInQueryIndex, ref MoveTo moveTo, ref VisibleCursor visibleCursor, in MouseClick mouseClick, in WorldClick worldClick, in DynamicBuffer<PlayerCursors> cursors) =>
            {
                if (mouseClick.CapturedThisFrame)
                {
                    moveTo.Stopped = false;
                    moveTo.Position = worldClick.WorldPosition;

                }
                visibleCursor.Cursor = CursorType.Movement;
            }).ScheduleParallel();

            Entities
            .WithNone<DisabledControl>()
            .WithAll<PlayerControlled>()
            .ForEach((Entity player, ref Fighter fighter, ref VisibleCursor visibleCursor, in MouseClick mouseClick) =>
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
                if (fighter.TargetFoundThisFrame > 0)
                {
                    visibleCursor.Cursor = CursorType.Combat;
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


    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class NoInteractionSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            // var cb = entityCommandBufferSystem.CreateCommandBuffer();
            // var ecb = cb.AsParallelWriter();
            Entities
            .WithNone<DisabledControl>()
            .WithNone<WorldClick>()
            .WithAny<PlayerControlled>()
            .ForEach((int entityInQueryIndex, Entity e, ref VisibleCursor visibleCursor, in Fighter f, in DynamicBuffer<PlayerCursors> cursors) =>
            {
                if (f.TargetFoundThisFrame == 0)
                {

                    visibleCursor.Cursor = CursorType.None;
                }
            }).ScheduleParallel();

            // entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}

