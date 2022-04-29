
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.AI;
using Unity.Mathematics;
using RPG.Core;
using UnityEngine;
using Unity.AI.Navigation;

namespace RPG.Mouvement
{
    [UpdateInGroup(typeof(MouvementSystemGroup))]
    public partial class IsMovingSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery isMovingQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithStoreEntityQueryInField(ref isMovingQuery)
            // .WithChangeFilter<MoveTo>()
            .WithNone<IsDeadTag, IsMoving>()
            .ForEach((int entityInQueryIndex, Entity e, in MoveTo moveTo) =>
            {
                if (!moveTo.Stopped)
                {
                    commandBuffer.AddComponent<IsMoving>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    //FIXME: split in smaller system
    [UpdateInGroup(typeof(MouvementSystemGroup))]
    [UpdateAfter(typeof(IsMovingSystem))]
    public partial class MoveToSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {

            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            // Calcule distance 
            Entities
           .WithChangeFilter<MoveTo>()
           .WithNone<IsDeadTag>()
           .WithAny<IsMoving>()
           .ForEach((int entityInQueryIndex, Entity e, ref MoveTo moveTo, in Translation t) =>
           {
               if (!moveTo.UseDirection)
               {
                   moveTo.Distance = math.distance(moveTo.Position, t.Value);
                   if (moveTo.Distance <= moveTo.StoppingDistance)
                   {
                       moveTo.Stopped = true;
                       moveTo.Position = t.Value;
                   }
               }

           }).ScheduleParallel();

            Entities
           //    .WithChangeFilter<MoveTo>()
           .WithAny<IsMoving>()
           .WithNone<IsDeadTag>()
           .ForEach((int entityInQueryIndex, Entity e, ref Mouvement mouvement, in MoveTo moveTo) =>
           {
               if (moveTo.Stopped)
               {
                   commandBuffer.RemoveComponent<IsMoving>(entityInQueryIndex, e);
                   mouvement.Velocity = new Velocity { Linear = float3.zero, Angular = float3.zero };
               }

           }).ScheduleParallel();


            // Initialize move to when spawned
            Entities
            .WithAll<Spawned>()
            .ForEach((ref Translation position, ref MoveTo moveTo) =>
            {
                moveTo.Position = position.Value;
            }).ScheduleParallel();

            Entities
            .WithAll<WarpTo, Warped>()
            .ForEach((int entityInQueryIndex, Entity e) =>
            {
                commandBuffer.RemoveComponent<WarpTo>(entityInQueryIndex, e);
                commandBuffer.RemoveComponent<Warped>(entityInQueryIndex, e);
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

        }
    }
    [UpdateInGroup(typeof(MouvementSystemGroup))]
    [UpdateAfter(typeof(MoveToSystem))]
    public partial class MoveToNavMeshAgentSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery navMeshAgentQueries;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {

            // TODO : Refractor get in paralle
            var lookAts = GetComponentDataFromEntity<LookAt>(true);

            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            // Warp when spawned or warp to
            Entities
            .WithAny<Spawned, WarpTo>()
            .ForEach((Entity e, ref MoveTo moveTo, in Translation position) =>
            {
                moveTo.Position = position.Value;
                commandBuffer.AddComponent<Warped>(e);
            }).Schedule();
            var dt = Time.DeltaTime;
            Entities
            .WithAny<IsMoving>()
            .WithNone<IsDeadTag, WarpTo>()
            .WithChangeFilter<MoveTo>()
            .WithReadOnly(lookAts)
            .WithStoreEntityQueryInField(ref navMeshAgentQueries)
            .ForEach((Entity e,
            ref Translation position,
            ref Mouvement mouvement,
            ref MoveTo moveTo,
            ref Rotation rotation,
            in LocalToWorld localToWorld) =>
            {
                bool isDirection = moveTo.UseDirection;
                NavMeshPath path = new NavMeshPath();
                // agent.path.ClearCorners();
                if (isDirection)
                {
                    moveTo.Direction = math.normalizesafe(moveTo.Direction);
                    var newPosition = moveTo.Direction + (moveTo.StoppingDistance * math.sign(moveTo.Direction));
                    NavMesh.SamplePosition((float3)position.Value + newPosition, out var hit, 10f, NavMesh.AllAreas);
                    if (hit.hit)
                    {
                        moveTo.Position = hit.position;
                    }
                    moveTo.Direction = float3.zero;
                    moveTo.UseDirection = false;
                }
                if (NavMesh.CalculatePath(position.Value, moveTo.Position, NavMesh.AllAreas, path))
                {
                    if (path.corners.Length >= 2)
                    {
                        var speed = moveTo.CalculeSpeed(in mouvement);
                        var direction = (float3)path.corners[1] - position.Value;
                        var step = speed * dt; // calculate distance to move

                        var moveToward = Vector3.MoveTowards(position.Value, path.corners[1], step);
                        var lookAt = Quaternion.LookRotation(direction, math.up());
                        mouvement.Velocity = new Velocity
                        {
                            Linear = new float3(0, 0, 1f) * mouvement.Speed,
                            Angular = new float3(0, 0, 0f)
                        };
                        if (!lookAts.HasComponent(e) || lookAts[e].Entity == Entity.Null)
                        {
                            rotation.Value = lookAt;
                        }
                        position.Value = moveToward;
                    }

                }
                // if (!agent.isOnNavMesh)
                // {

                //     // agent.Warp(position.Value);
                //     agent.transform.position = position.Value;
                //     // Debug.LogWarning($"NavMeshSurface Not Found {e.Index} {agent.isOnNavMesh}");
                // }
            }).Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

}
