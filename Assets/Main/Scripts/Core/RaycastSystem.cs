
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine.EventSystems;

namespace RPG.Core
{
    public struct Raycast : IComponentData
    {
        public Ray Ray;
        public float Distance;

        public bool Completed;
    }
    public struct HittedByRaycastEvent : IBufferElementData
    {
        public RaycastHit Hit;
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

        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery rayCastQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            // entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                var physicsWorld = buildPhysicsWorld.PhysicsWorld;
                var collisionWorld = physicsWorld.CollisionWorld;
                // var cb = entityCommandBufferSystem.CreateCommandBuffer();
                // var cbp = cb.AsParallelWriter();
                Dependency = JobHandle.CombineDependencies(Dependency, buildPhysicsWorld.GetOutputDependency(), stepPhysicsWorld.GetOutputDependency());
                EntityManager.RemoveComponent<InteractWithUI>(rayCastQuery);
                var raycastJob = Entities
                .WithReadOnly(physicsWorld)
                .WithReadOnly(collisionWorld)
                .WithChangeFilter<Raycast>()
                .WithStoreEntityQueryInField(ref rayCastQuery)
                .ForEach((int entityInQueryIndex, ref Raycast raycast, ref DynamicBuffer<HittedByRaycastEvent> rayHits) =>
                {
                    if (!raycast.Completed)
                    {
                        RaycastInput input = new RaycastInput() { Start = raycast.Ray.Origin, End = raycast.Ray.Displacement * raycast.Distance, Filter = CollisionFilter.Default };
                        var hits = new NativeList<RaycastHit>(Allocator.Temp);
                        collisionWorld.CastRay(input, ref hits);
                        for (int i = 0; i < hits.Length; i++)
                        {
                            var hittedEntity = physicsWorld.Bodies[hits[i].RigidBodyIndex].Entity;
                            rayHits.Add(new HittedByRaycastEvent { Hit = hits[i], Hitted = hittedEntity });
                            // cbp.AddComponent<HittedByRaycast>(entityInQueryIndex, hittedEntity);
                        }
                        raycast.Completed = true;
                    }

                }).ScheduleParallel(Dependency);
                Dependency = raycastJob;
                // entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            }
            else
            {
                EntityManager.AddComponent<InteractWithUI>(rayCastQuery);
            }

        }
    }
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public class EndSimulationRaycastHitSystem : SystemBase
    {
        EndSimulationEntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = commandBufferSystem.CreateCommandBuffer();
            Entities.ForEach((ref DynamicBuffer<HittedByRaycastEvent> rayHits) => { rayHits.Clear(); }).ScheduleParallel();
        }
    }
}

