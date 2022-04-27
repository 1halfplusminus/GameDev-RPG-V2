using RPG.Combat;
using RPG.Mouvement;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Core;
using RPG.Animation;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Systems;

namespace RPG.Control
{
    [UpdateInGroup(typeof(ControlSystemGroup))]
    [UpdateAfter(typeof(SuspiciousSystem))]
    public partial class PatrolBehaviourSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var waypointsByPath = GetBufferFromEntity<PatrolWaypoint>(true);
            Entities
            .WithReadOnly(waypointsByPath)
            .WithChangeFilter<PatrollingPath>()
            .ForEach((ref Patrolling patrolling, in PatrollingPath path) =>
            {
                var waypoints = waypointsByPath[path.Entity];
                patrolling.Start(waypoints.Length);
            }).ScheduleParallel();
            Entities
            .WithReadOnly(waypointsByPath)
            .WithChangeFilter<AutoStartPatroling>()
            .WithAll<AutoStartPatroling>().ForEach((ref Patrolling patrolling, in PatrollingPath path) =>
            {
                Debug.Log("Auto Start Patrolling");
                var waypoints = waypointsByPath[path.Entity];
                patrolling.Start(waypoints.Length);
            }).ScheduleParallel();
            Entities
            .WithReadOnly(waypointsByPath)
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
    public partial class SuspiciousSystem : SystemBase
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
    public partial class GuardBehaviorSystem : SystemBase
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
    public partial class ChaseBehaviourSystem : SystemBase
    {
        EntityQuery playerControlledQuery;
        EntityQuery playerChaserQuery;
        EntityCommandBufferSystem entityCommandBufferSystem;
        BuildPhysicsWorld buildPhysicsWorld;
        StepPhysicsWorld stepPhysicsWorld;

        protected override void OnCreate()
        {
            base.OnCreate();
            playerChaserQuery = GetEntityQuery(typeof(ChasePlayer));
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            RequireForUpdate(playerChaserQuery);
        }
        protected override void OnStartRunning(){
            base.OnStartRunning();
            this.RegisterPhysicsRuntimeSystemReadOnly();
        }
        protected override void OnUpdate()
        {

            var physicsWorld = buildPhysicsWorld.PhysicsWorld;
            var collisionWorld = physicsWorld.CollisionWorld;
            // var playerPositions = new NativeHashMap<Entity, LocalToWorld>(playerControlledQuery.CalculateEntityCount(), Allocator.TempJob);
            // var playerPositionsWriter = playerPositions.AsParallelWriter();
            // Entities
            // .WithDisposeOnCompletion(playerPositionsWriter)
            // .WithAll<PlayerControlled>()
            // .WithStoreEntityQueryInField(ref playerControlledQuery)
            // .ForEach((Entity e, in LocalToWorld position) =>
            // {
            //     playerPositionsWriter.TryAdd(e, position);
            // }).ScheduleParallel();
            //TODO: Refractor with a event system create a event when target lost & when target aquired
            var beginSimulationEntityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
                       .WithNone<IsChasingTarget, Spawned, IsDeadTag>()
                       .WithReadOnly(physicsWorld)
                       .WithReadOnly(collisionWorld)
                       // .WithReadOnly(playerPositions)
                       .ForEach((int entityInQueryIndex, Entity e, in ChasePlayer chasePlayer, in LocalToWorld localToWorld, in Rotation rotation) =>
                       {
                           var pointDistanceInput = new PointDistanceInput { Position = localToWorld.Position, MaxDistance = chasePlayer.ChaseDistance, Filter = chasePlayer.Filter };
                           NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
                           collisionWorld.CalculateDistance(pointDistanceInput, ref hits);
                           for (int i = 0; i < hits.Length; i++)
                           {

                               var hit = hits[i];
                               //    var playerPosition = hit.Position;
                               var entity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                               Debug.Log($"Chase Target found {entity.Index}");
                               if (HasComponent<LocalToWorld>(entity))
                               {
                                   var playerLocalToWorld = GetComponent<LocalToWorld>(entity);
                                   var playerPosition = playerLocalToWorld.Position;

                                   var forwardVector = math.forward(rotation.Value);
                                   var vectorToPlayer = playerPosition - localToWorld.Position;
                                   // var distance = math.lengthsq(vectorToPlayer);
                                   var unitVecToPlayer = math.normalize(vectorToPlayer);
                                   var angleRadians = math.radians(chasePlayer.AngleOfView);
                                   // Use the dot product to determine if the player is within our vision cone
                                   var dot = math.dot(forwardVector, unitVecToPlayer);
                                   var canSeePlayer = dot > 0.0f && // player is in front of us
                                       math.abs(math.acos(dot)) < angleRadians;

                                   if (canSeePlayer)
                                   {
                                       beginSimulationEntityCommandBuffer.AddComponent<IsChasingTarget>(entityInQueryIndex, e);
                                       beginSimulationEntityCommandBuffer.AddComponent(entityInQueryIndex, e, new StartChaseTarget { Target = entity, Position = playerPosition });
                                       break;
                                   }
                               }

                           }
                           hits.Dispose();
                           // var localToWorlds = playerPositions.GetValueArray(Allocator.Temp);
                           // var entities = playerPositions.GetKeyArray(Allocator.Temp);

                           // for (int i = 0; i < localToWorlds.Length; i++)
                           // {
                           //     var playerLocalToWorld = localToWorlds[i];
                           //     var entity = entities[i];

                           //     var forwardVector = math.forward(rotation.Value);
                           //     var vectorToPlayer = playerLocalToWorld.Position - localToWorld.Position;
                           //     var distance = math.lengthsq(vectorToPlayer);
                           //     var unitVecToPlayer = math.normalize(vectorToPlayer);
                           //     var angleRadians = math.radians(chasePlayer.AngleOfView);
                           //     // Use the dot product to determine if the player is within our vision cone
                           //     var dot = math.dot(forwardVector, unitVecToPlayer);
                           //     var canSeePlayer = dot > 0.0f && // player is in front of us
                           //         math.abs(math.acos(dot)) < angleRadians;

                           //     if (distance <= chasePlayer.ChaseDistanceSq && canSeePlayer)
                           //     {
                           //         beginSimulationEntityCommandBuffer.AddComponent<IsChasingTarget>(entityInQueryIndex, e);
                           //         beginSimulationEntityCommandBuffer.AddComponent(entityInQueryIndex, e, new StartChaseTarget { Target = entity, Position = playerLocalToWorld.Position });
                           //     }
                           // }
                       }).ScheduleParallel();

            // chasePlayer.Complete();

            // Dependency = chasePlayer;

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
            // .WithReadOnly(playerPositions)
            // .WithDisposeOnCompletion(playerPositions)
            .ForEach((int entityInQueryIndex, Entity e, in ChasePlayer chasePlayer, in LocalToWorld localToWorld) =>
            {
                var currentTarget = chasePlayer.Target;
                var playerLocalToWorld = GetComponent<LocalToWorld>(currentTarget);
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


            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

        }
    }

    [UpdateInGroup(typeof(ControlSystemGroup))]
    public partial class AIAnimationSystem : SystemBase
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
           .WithNone<IsSuspicious>()
           .WithChangeFilter<GuardAnimation>().ForEach((ref GuardAnimation animation) =>
             {
                 animation.NervouslyLookingAround = 0.0f;
             }).ScheduleParallel();
        }
    }
}