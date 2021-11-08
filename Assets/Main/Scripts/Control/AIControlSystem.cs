using RPG.Combat;
using RPG.Mouvement;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Core;
using RPG.Combat;
using RPG.Animation;
using UnityEngine;

namespace RPG.Control
{
    [UpdateInGroup(typeof(ControlSystemGroup))]
    [UpdateAfter(typeof(SuspiciousSystem))]
    public class PatrolBehaviourSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var waypointsByPath = GetBufferFromEntity<PatrolWaypoint>(true);
            Entities.WithReadOnly(waypointsByPath).WithAll<Spawned>().ForEach((ref Patrolling patrolling, in PatrollingPath path) =>
            {
                var waypoints = waypointsByPath[path.Entity];
                patrolling.Start(waypoints.Length);
            }).ScheduleParallel();
            Entities.WithReadOnly(waypointsByPath)
            .WithChangeFilter<AutoStartPatroling>()
            .WithAll<AutoStartPatroling>().ForEach((ref Patrolling patrolling, in PatrollingPath path) =>
            {
                Debug.Log("Auto Start Patrolling");
                var waypoints = waypointsByPath[path.Entity];
                patrolling.Start(waypoints.Length);
            }).ScheduleParallel();
            Entities.WithReadOnly(waypointsByPath)
            .WithNone<IsSuspicious, IsChasingTarget, IsFighting>()
            .ForEach((ref Patrolling patrolling, ref MoveTo moveTo, ref Suspicious suspicious, in PatrollingPath path, in LocalToWorld localToWorld) =>
            {
                var waypoints = waypointsByPath[path.Entity];

                if (patrolling.Started)
                {
                    Patrol(ref patrolling, ref moveTo, localToWorld, waypoints);
                    if (patrolling.IsDwelling)
                    {
                        moveTo.Stopped = true;
                        suspicious.Start(patrolling.DwellingTime);
                        Debug.Log("arrivedAtWaypoint move to STOPPED");
                    }
                }

            }).ScheduleParallel();
        }

        private static void Patrol(ref Patrolling patrolling, ref MoveTo moveTo, in LocalToWorld localToWorld, in DynamicBuffer<PatrolWaypoint> waypoints)
        {
            var currentWaypont = GetPosition(patrolling, waypoints);
            patrolling.Update(localToWorld.Position, currentWaypont, out bool wasDwelling);
            if (!wasDwelling)
            {
                MoveToWaypoint(patrolling, ref moveTo, waypoints);
            }
        }

        private static void MoveToWaypoint(in Patrolling patrolling, ref MoveTo moveTo, in DynamicBuffer<PatrolWaypoint> waypoints)
        {
            moveTo.Stopped = false;
            moveTo.SpeedPercent = patrolling.PatrolSpeed;
            moveTo.Position = GetPosition(patrolling, waypoints);
        }

        private static float3 GetPosition(in Patrolling patrolling, in DynamicBuffer<PatrolWaypoint> waypoints)
        {
            return waypoints[patrolling.CurrentWayPoint].Position;
        }
    }
    [UpdateInGroup(typeof(ControlSystemGroup))]
    [UpdateAfter(typeof(ChaseBehaviourSystem))]
    public class SuspiciousSystem : SystemBase
    {
        EntityCommandBufferSystem ecs;
        protected override void OnCreate()
        {
            base.OnCreate();
            ecs = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = ecs.CreateCommandBuffer().AsParallelWriter();

            Entities.WithAll<ChaseTargetLose>().ForEach((int entityInQueryIndex, Entity e, ref Suspicious suspicious) =>
            {
                suspicious.Start();
                commandBuffer.AddComponent<IsSuspicious>(entityInQueryIndex, e);
            }).ScheduleParallel();
            Entities.WithAll<StartChaseTarget>().ForEach((int entityInQueryIndex, Entity e, ref Suspicious suspicious) =>
            {
                suspicious.Finish();
                commandBuffer.RemoveComponent<IsSuspicious>(entityInQueryIndex, e);
            }).ScheduleParallel();
            Entities.WithNone<IsSuspicious>().ForEach((int entityInQueryIndex, Entity e, in Suspicious suspicious) =>
            {
                if (suspicious.StartedThisFrame)
                {
                    Debug.Log("suspicious started this frame");
                    commandBuffer.AddComponent<IsSuspicious>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();
            Entities.WithAny<IsSuspicious>().ForEach((int entityInQueryIndex, Entity e, ref Suspicious suspicious, in DeltaTime time) =>
            {
                suspicious.Update(time.Value);
                if (suspicious.IsFinish)
                {
                    Debug.Log("Suspicious is finish");
                    suspicious.Reset();
                    commandBuffer.RemoveComponent<IsSuspicious>(entityInQueryIndex, e);
                }

            }).ScheduleParallel();

            ecs.AddJobHandleForProducer(Dependency);
        }
    }
    [UpdateInGroup(typeof(ControlSystemGroup))]
    [UpdateAfter(typeof(ChaseBehaviourSystem))]
    public class GuardBehaviorSystem : SystemBase
    {

        protected override void OnCreate()
        {
            base.OnCreate();

        }
        protected override void OnUpdate()
        {
            Entities.WithAll<Spawned, GuardOriginalLocationTag>().ForEach((ref GuardLocation guardLocation, in Translation translation) =>
            {
                guardLocation.Value = translation.Value;
            }).ScheduleParallel();

            Entities.
             WithNone<Spawned, IsSuspicious, IsFighting>()
            .WithNone<IsChasingTarget>()
            .ForEach((ref MoveTo moveTo, in GuardLocation guardLocation) =>
            {
                moveTo.Position = guardLocation.Value;
                moveTo.Stopped = false;
            }).ScheduleParallel();

        }
    }
    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class ChaseBehaviourSystem : SystemBase
    {
        EntityQuery playerControlledQuery;

        EntityCommandBufferSystem beginSimulationEntityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            playerControlledQuery = GetEntityQuery(typeof(PlayerControlled), ComponentType.ReadOnly<LocalToWorld>());
            beginSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {

            var playerPositions = new NativeHashMap<Entity, LocalToWorld>(playerControlledQuery.CalculateEntityCount(), Allocator.TempJob);
            var playerPositionsWriter = playerPositions.AsParallelWriter();
            Entities
            .WithDisposeOnCompletion(playerPositionsWriter)
            .WithAll<PlayerControlled>()
            .ForEach((Entity e, in LocalToWorld position) =>
            {
                playerPositionsWriter.TryAdd(e, position);
            }).ScheduleParallel();
            // Todo: Refractor with a event system create a event when target lost & when target aquired
            var beginSimulationEntityCommandBuffer = beginSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithNone<IsChasingTarget, Spawned>()
            .WithReadOnly(playerPositions)
            .ForEach((int entityInQueryIndex, Entity e, in ChasePlayer chasePlayer, in LocalToWorld localToWorld) =>
            {
                var localToWorlds = playerPositions.GetValueArray(Allocator.Temp);
                var entities = playerPositions.GetKeyArray(Allocator.Temp);

                for (int i = 0; i < localToWorlds.Length; i++)
                {
                    var playerLocalToWorld = localToWorlds[i];
                    var entity = entities[i];
                    if (math.abs(math.distance(localToWorld.Position, playerLocalToWorld.Position)) <= chasePlayer.ChaseDistance)
                    {
                        beginSimulationEntityCommandBuffer.AddComponent<IsChasingTarget>(entityInQueryIndex, e);
                        beginSimulationEntityCommandBuffer.AddComponent(entityInQueryIndex, e, new StartChaseTarget { Target = entity, Position = playerLocalToWorld.Position });
                    }
                }
            }).ScheduleParallel();

            Entities.ForEach((ref ChasePlayer chasePlayer, in StartChaseTarget startChaseTarget) =>
           {
               chasePlayer.Target = startChaseTarget.Target;
           }).ScheduleParallel();

            Entities.ForEach((ref Fighter fighter, in StartChaseTarget startChaseTarget) =>
            {
                fighter.Target = startChaseTarget.Target;
            }).ScheduleParallel();

            Entities.WithAll<IsChasingTarget>().ForEach((ref MoveTo moveTo, in StartChaseTarget startChaseTarget) =>
            {
                moveTo.SpeedPercent = 1f;
                moveTo.Position = startChaseTarget.Position;
                moveTo.Stopped = false;
            }).ScheduleParallel();

            Entities.WithAll<IsChasingTarget>().ForEach((ref Fighter fighter) =>
            {
                if (!fighter.TargetInRange)
                {
                    fighter.MoveTowardTarget = true;
                }
            }).ScheduleParallel();

            Entities.WithAll<StartChaseTarget>().ForEach((int entityInQueryIndex, Entity entity) =>
            {
                beginSimulationEntityCommandBuffer.RemoveComponent<StartChaseTarget>(entityInQueryIndex, entity);
            }).ScheduleParallel();

            Entities
            .WithAny<IsChasingTarget>()
            .WithNone<Spawned>()
            .WithReadOnly(playerPositions)
            .WithDisposeOnCompletion(playerPositions)
            .ForEach((int entityInQueryIndex, Entity e, in ChasePlayer chasePlayer, in LocalToWorld localToWorld) =>
            {
                var currentTarget = chasePlayer.Target;
                var playerLocalToWorld = playerPositions[currentTarget];
                if (math.abs(math.distance(localToWorld.Position, playerLocalToWorld.Position)) >= chasePlayer.ChaseDistance)
                {
                    beginSimulationEntityCommandBuffer.RemoveComponent<IsChasingTarget>(entityInQueryIndex, e);
                    beginSimulationEntityCommandBuffer.AddComponent(entityInQueryIndex, e, new ChaseTargetLose { Target = currentTarget });
                }
            }).ScheduleParallel();


            Entities.WithAll<ChaseTargetLose>().ForEach((ref ChasePlayer chasePlayer) =>
            {
                chasePlayer.Target = Entity.Null;
            }).ScheduleParallel();
            Entities.WithAll<ChaseTargetLose>().ForEach((ref Fighter fighter) =>
            {
                fighter.Target = Entity.Null;
                fighter.MoveTowardTarget = false;
            }).ScheduleParallel();

            Entities.WithAll<ChaseTargetLose>().ForEach((ref LookAt lookAt) =>
            {
                lookAt.Entity = Entity.Null;
            }).ScheduleParallel();

            Entities.WithAll<ChaseTargetLose>().ForEach((int entityInQueryIndex, Entity entity) =>
            {
                beginSimulationEntityCommandBuffer.RemoveComponent<ChaseTargetLose>(entityInQueryIndex, entity);
            }).ScheduleParallel();

            beginSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);

        }
    }


    public class AIAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithAny<IsSuspicious>().ForEach((ref GuardAnimation animation) =>
            {
                animation.NervouslyLookingAround += 0.1f;
                animation.NervouslyLookingAround = math.min(animation.NervouslyLookingAround, 1.0f);
            }).ScheduleParallel();

            Entities
           .WithNone<IsSuspicious>().WithChangeFilter<GuardAnimation>().ForEach((ref GuardAnimation animation) =>
           {
               animation.NervouslyLookingAround = math.max(animation.NervouslyLookingAround - 0.1f, 0.0f);
           }).ScheduleParallel();
        }
    }
}