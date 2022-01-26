using Unity.Entities;
using RPG.Core;
using RPG.Mouvement;
using RPG.Combat;
using UnityEngine.AI;
using Unity.Transforms;
using Unity.Mathematics;

namespace RPG.Control
{
    public struct PlayerControlled : IComponentData { }
    public struct DisabledControl : IComponentData { }
    public struct HasPathToTarget : IComponentData { }
    [UpdateInGroup(typeof(ControlSystemGroup))]
    [UpdateAfter(typeof(MovementClickInteractionSystem))]
    public class CombatClickInteractionSystem : SystemBase
    {

        protected override void OnCreate()
        {
            base.OnCreate();
        }
        protected override void OnUpdate()
        {
            Entities
            .WithNone<DisabledControl, InteractWithUI>()
            .WithAll<PlayerControlled>()
            .ForEach((Entity player, ref Fighter fighter, ref VisibleCursor visibleCursor, in WorldClick worldClick, in MouseClick mouseClick, in LocalToWorld localToWorld) =>
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

                    if (HasComponent<HasPathToTarget>(player) || math.abs(math.distance(localToWorld.Position, worldClick.WorldPosition)) <= fighter.Range)
                    {
                        visibleCursor.Cursor = CursorType.Combat;
                    }
                }
            }).ScheduleParallel();
            // Look at fighter target if exists
            Entities
            .WithNone<DisabledControl, InteractWithUI>()
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
    public class MovementClickInteractionSystem : SystemBase
    {
        EntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        private static float CalculeDistance(NavMeshPath navMeshPath)
        {
            var distance = 0f;
            if (navMeshPath.corners.Length < 2) return distance;
            for (int i = 0; i < navMeshPath.corners.Length - 1; i++)
            {
                distance += math.abs(math.distance(navMeshPath.corners[i], navMeshPath.corners[i + 1]));
            }
            return distance;
        }
        protected override void OnUpdate()
        {
            var cb = commandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();

            Entities
            .WithAny<DisabledControl, InteractWithUI>()
            .ForEach((ref MoveTo moveTo) =>
            {
                moveTo.Stopped = true;

            }).ScheduleParallel();

            Entities
            .WithNone<InteractWithUI>()
            // .WithChangeFilter<WorldClick>()
            .WithAll<PlayerControlled>()
            .ForEach((Entity e, ref MoveTo moveTo, ref VisibleCursor cursor, ref WorldClick worldClick, in Raycast raycast, in LocalToWorld localToWorld) =>
            {
                if (worldClick.Frame <= 1)
                {
                    var havePathToTarget = false;
                    NavMesh.SamplePosition(worldClick.WorldPosition, out var hit, raycast.MaxNavMeshProjectionDistance, NavMesh.AllAreas);
                    if (hit.hit)
                    {
                        var nashMeshPath = new NavMeshPath();
                        NavMesh.CalculatePath(localToWorld.Position, hit.position, NavMesh.AllAreas, nashMeshPath);
                        if (nashMeshPath.status == NavMeshPathStatus.PathComplete && CalculeDistance(nashMeshPath) <= raycast.MaxNavPathLength)
                        {
                            havePathToTarget = true;
                            worldClick.WorldPosition = hit.position;
                            EntityManager.AddComponent<HasPathToTarget>(e);
                        }
                    }
                    if (!havePathToTarget)
                    {
                        EntityManager.RemoveComponent<HasPathToTarget>(e);
                    }
                }
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            Entities
           .WithNone<InteractWithUI>()
           .WithAll<PlayerControlled, HasPathToTarget>()
           .ForEach((Entity e, ref MoveTo moveTo, ref VisibleCursor cursor, ref WorldClick worldClick, in Raycast raycast, in LocalToWorld localToWorld, in MouseClick mouseClick) =>
           {
               if (mouseClick.CapturedThisFrame)
               {
                   moveTo.Stopped = false;
                   moveTo.Position = worldClick.WorldPosition;
               }
               cursor.Cursor = CursorType.Movement;
           })
           .ScheduleParallel();

            commandBufferSystem.AddJobHandleForProducer(Dependency);
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
            .WithNone<InteractWithUI>()
            .WithAll<PlayerControlled>()
            // .WithChangeFilter<MouseClick>()
            .ForEach((ref Raycast cast, in MouseClick mouseClick) =>
            {
                if (mouseClick.Frame <= 1)
                {
                    cast.Completed = false;
                    cast.Ray = mouseClick.Ray;
                }

            }).ScheduleParallel();
        }
    }


    [UpdateInGroup(typeof(ControlSystemGroup))]
    [UpdateAfter(typeof(MovementClickInteractionSystem))]
    public class NoInteractionSystem : SystemBase
    {

        protected override void OnCreate()
        {
            base.OnCreate();

        }
        protected override void OnUpdate()
        {

            Entities
            .WithNone<DisabledControl, InteractWithUI, HasPathToTarget>()
            .WithAny<PlayerControlled>()
            .ForEach((int entityInQueryIndex, Entity e, ref VisibleCursor visibleCursor, in Fighter f) =>
            {
                if (f.TargetFoundThisFrame == 0)
                {
                    visibleCursor.Cursor = CursorType.None;
                }
            }).ScheduleParallel();

        }
    }
}

