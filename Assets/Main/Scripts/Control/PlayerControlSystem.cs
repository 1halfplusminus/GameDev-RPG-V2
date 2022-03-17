using Unity.Entities;
using RPG.Core;
using RPG.Mouvement;
using RPG.Combat;
using UnityEngine.AI;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Jobs;
using Unity.Physics;
using Unity.Collections;
using Unity.Physics.Authoring;
using Unity.Assertions;

namespace RPG.Control
{
    public struct ComponentClosestHitCollector<T, V> : ICollector<T> where T : struct, IQueryResult where V : struct, IComponentData
    {
        [ReadOnly]
        public ComponentDataFromEntity<V> ComponentDataFromEntity;
        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; private set; }
        public int NumHits { get; private set; }

        private T m_ClosestHit;
        public T ClosestHit => m_ClosestHit;

        public ComponentClosestHitCollector(float maxFraction, [ReadOnly] ComponentDataFromEntity<V> datas)
        {
            ComponentDataFromEntity = datas;
            MaxFraction = maxFraction;
            m_ClosestHit = default;
            NumHits = 0;
        }

        #region ICollector

        public bool AddHit(T hit)
        {
            var hasComponent = ComponentDataFromEntity.HasComponent(hit.Entity);
            Assert.IsTrue(hasComponent);
            if (hasComponent)
            {
                Assert.IsTrue(hit.Fraction <= MaxFraction);
                MaxFraction = hit.Fraction;
                m_ClosestHit = hit;
                NumHits = 1;
            }
            return true;
        }

        #endregion
    }
    public struct PlayerControlled : IComponentData { }
    public struct DisabledControl : IComponentData { }
    public struct HasPathToTarget : IComponentData { }
    public struct AttackClosestTarget : IComponentData { }

    [UpdateInGroup(typeof(ControlSystemGroup))]
    // [UpdateBefore(typeof(NoInteractionSystem))]
    public class AttackClosestTargetSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        BuildPhysicsWorld buildPhysicsWorld;
        StepPhysicsWorld stepPhysicsWorld;

        protected override void OnCreate()
        {
            base.OnCreate();
            stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            Dependency = JobHandle.CombineDependencies(Dependency, buildPhysicsWorld.GetOutputDependency(), stepPhysicsWorld.GetOutputDependency());
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            var physicsWorld = buildPhysicsWorld.PhysicsWorld;
            var collisionWorld = physicsWorld.CollisionWorld;
            var category0 = new PhysicsCategoryTags
            {
                Category00 = true
            };
            var category8 = new PhysicsCategoryTags
            {
                Category08 = true
            };

            Entities
            .WithNone<DisabledControl>()
            .WithReadOnly(collisionWorld)
            .WithAll<AttackClosestTarget>()
            .ForEach((int entityInQueryIndex, Entity e, ref Fighter fighter, in Translation translation, in Raycast raycast) =>
            {
                var hittables = GetComponentDataFromEntity<Hittable>(true);
                var maxDistance = 8f;
                var pointDistanceInput = new PointDistanceInput
                {
                    Position = translation.Value,
                    MaxDistance = maxDistance,
                    Filter = new CollisionFilter { BelongsTo = category0.Value, CollidesWith = category8.Value }
                };
                var hits = new ComponentClosestHitCollector<DistanceHit, Hittable>(maxDistance + 4f, hittables);
                collisionWorld.CalculateDistance(pointDistanceInput, ref hits);
                var hit = hits.ClosestHit;
                if (hit.Entity != Entity.Null)
                {
                    fighter.Target = hit.Entity;
                    fighter.MoveTowardTarget = true;
                    fighter.TargetFoundThisFrame = 1;
                    if (HasComponent<LookAt>(e))
                    {
                        var lookAt = GetComponent<LookAt>(e);
                        lookAt.Entity = hit.Entity;
                        cbp.AddComponent(entityInQueryIndex, e, lookAt);
                    }
                }
                cbp.RemoveComponent<AttackClosestTarget>(entityInQueryIndex, e);
            }).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

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
            .WithAny<DisabledControl>()
            .ForEach((ref MoveTo moveTo) =>
            {
                moveTo.Stopped = true;
            }).ScheduleParallel();

            Entities
            .WithNone<InteractWithUI>()
            .WithChangeFilter<MouseClick>()
            .WithAll<PlayerControlled>()
            .ForEach((Entity e, ref WorldClick worldClick, in Raycast raycast, in LocalToWorld localToWorld) =>
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
                        cb.AddComponent<HasPathToTarget>(e);
                    }
                }
                if (!havePathToTarget)
                {
                    cb.RemoveComponent<HasPathToTarget>(e);
                }
            })
            .WithoutBurst()
            .Run();

            Entities
           .WithNone<InteractWithUI, DisabledControl>()
           .WithAll<PlayerControlled, HasPathToTarget>()
           .ForEach((ref MoveTo moveTo, ref VisibleCursor cursor, ref WorldClick worldClick, in Raycast raycast, in LocalToWorld localToWorld, in MouseClick mouseClick) =>
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
        protected override void OnUpdate()
        {
            Entities
            .WithNone<InteractWithUI>()
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
    [UpdateAfter(typeof(MovementClickInteractionSystem))]
    public class NoInteractionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithAny<DisabledControl, InteractWithUI, WorldClick>()
            .WithNone<HasPathToTarget>()
            .WithAll<PlayerControlled>()
            .ForEach((ref VisibleCursor visibleCursor, in Fighter f) =>
            {
                if (f.TargetFoundThisFrame == 0)
                {
                    visibleCursor.Cursor = CursorType.None;
                }
            }).ScheduleParallel();
        }
    }
}
