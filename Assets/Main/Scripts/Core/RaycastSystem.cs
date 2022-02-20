
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine.EventSystems;

namespace RPG.Core
{
    public struct Raycast : IComponentData
    {
        public Ray Ray;
        public float Distance;

        public float MaxNavPathLength;
        public float MaxNavMeshProjectionDistance;
        public bool Completed;
        public float Radius;
        public CollisionFilter CollisionFilter;
    }
    public struct HittedByRaycastEvent : IBufferElementData
    {
        public float3 Position;
        public Entity Hitted;

        public Entity Hitter;
    }

    public struct HittedByRaycast : IComponentData
    {

    }
    public struct InteractWithUI : IComponentData
    {

    }

    [UpdateAfter(typeof(MouseInputSystem))]
    [UpdateInGroup(typeof(CoreSystemGroup))]
    public class RaycastSystem : SystemBase
    {
        BuildPhysicsWorld buildPhysicsWorld;
        StepPhysicsWorld stepPhysicsWorld;

        EntityQuery rayCastQuery;
        EntityQuery interactionWithUIEntityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            interactionWithUIEntityQuery = GetEntityQuery(typeof(InteractWithUI));
        }
        protected override void OnUpdate()
        {
            if (EventSystem.current && !EventSystem.current.IsPointerOverGameObject())
            {
                EntityManager.RemoveComponent<InteractWithUI>(interactionWithUIEntityQuery);
                var physicsWorld = buildPhysicsWorld.PhysicsWorld;
                var collisionWorld = physicsWorld.CollisionWorld;
                Dependency = JobHandle.CombineDependencies(Dependency, buildPhysicsWorld.GetOutputDependency(), stepPhysicsWorld.GetOutputDependency());
                var raycastJob = Entities
                .WithReadOnly(physicsWorld)
                .WithReadOnly(collisionWorld)
                .WithChangeFilter<Raycast>()
                .WithStoreEntityQueryInField(ref rayCastQuery)
                .ForEach((int entityInQueryIndex, ref Raycast raycast, ref DynamicBuffer<HittedByRaycastEvent> rayHits) =>
                {
                    if (!raycast.Completed)
                    {
                        RaycastInput input = new RaycastInput
                        {
                            Start = raycast.Ray.Origin,
                            End = raycast.Ray.Displacement * raycast.Distance,
                            Filter = raycast.CollisionFilter
                        };
                        var hits = new NativeList<RaycastHit>(Allocator.Temp);
                        collisionWorld.CastRay(input, ref hits);
                        // collisionWorld.SphereCastAll(raycast.Ray.Origin, raycast.Radius, raycast.Ray.Displacement, raycast.Distance, ref hits, raycast.CollisionFilter);
                        for (int i = 0; i < hits.Length; i++)
                        {
                            var hittedEntity = physicsWorld.Bodies[hits[i].RigidBodyIndex].Entity;
                            rayHits.Add(new HittedByRaycastEvent { Position = hits[i].Position, Hitted = hittedEntity });
                        }
                        raycast.Completed = true;
                        hits.Dispose();
                    }

                }).ScheduleParallel(Dependency);
                Dependency = raycastJob;
            }
            else
            {
                EntityManager.AddComponent<InteractWithUI>(rayCastQuery);
            }

        }
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    [UpdateBefore(typeof(RaycastSystem))]
    public class EndSimulationRaycastHitSystem : SystemBase
    {
        EndSimulationEntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
        }
        protected override void OnUpdate()
        {
            Entities.ForEach((ref DynamicBuffer<HittedByRaycastEvent> rayHits) => { rayHits.Clear(); }).ScheduleParallel();
        }
    }
}

