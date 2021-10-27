
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;

namespace RPG.Core
{
    public struct Raycast : IComponentData
    {
        public Unity.Physics.Ray Ray;
        public float Distance;

        public bool Completed;
    }
    public struct HittedByRaycast : IBufferElementData
    {
        public RaycastHit Hit;
        public Entity Hitted;

        public Entity Hitter;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    [UpdateAfter(typeof(MouseInputSystem))]
    public class RaycastSystem : SystemBase
    {
        BuildPhysicsWorld buildPhysicsWorld;
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var physicsWorld = buildPhysicsWorld.PhysicsWorld;
            var collisionWorld = physicsWorld.CollisionWorld;
            var inputDeps = JobHandle.CombineDependencies(this.Dependency, buildPhysicsWorld.GetOutputDependency());

            var handle = Entities
            .WithReadOnly(physicsWorld)
            .WithReadOnly(collisionWorld)
            .WithChangeFilter<Raycast>()
            .ForEach((ref Raycast raycast, ref DynamicBuffer<HittedByRaycast> rayHits) =>
            {
                if (!raycast.Completed)
                {
                    RaycastInput input = new RaycastInput { Start = raycast.Ray.Origin, End = raycast.Ray.Displacement * raycast.Distance, Filter = CollisionFilter.Default };
                    var hits = new NativeList<RaycastHit>(Allocator.Temp);
                    collisionWorld.CastRay(input, ref hits);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var hittedEntity = physicsWorld.Bodies[hits[i].RigidBodyIndex].Entity;
                        rayHits.Add(new HittedByRaycast { Hit = hits[i], Hitted = hittedEntity });
                    }
                    raycast.Completed = true;
                }

            }).ScheduleParallel(inputDeps);
            handle.Complete();

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
            Entities.ForEach((ref DynamicBuffer<HittedByRaycast> rayHits) => { rayHits.Clear(); }).ScheduleParallel();
        }
    }
}

