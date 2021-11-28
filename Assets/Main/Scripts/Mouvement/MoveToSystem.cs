
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.AI;
using Unity.Mathematics;
using RPG.Core;
using Unity.AI.Navigation;
using UnityEngine;

namespace RPG.Mouvement
{
    public struct IsMoving : IComponentData { }
    [UpdateInGroup(typeof(MouvementSystemGroup))]

    public class MoveToSystem : SystemBase
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
            .ForEach((int entityInQueryIndex, Entity e, ref MoveTo moveTo, in LocalToWorld localToWorld) =>
            {
                moveTo.Distance = math.distance(moveTo.Position, localToWorld.Position);
                if (moveTo.Distance <= moveTo.StoppingDistance)
                {
                    moveTo.Stopped = true;
                    moveTo.Position = localToWorld.Position;
                }
            }).ScheduleParallel();

            Entities
           .WithChangeFilter<MoveTo>()
           .WithAny<IsMoving>()
           .WithNone<IsDeadTag>()
           .ForEach((int entityInQueryIndex, Entity e, ref MoveTo moveTo, ref Mouvement mouvement) =>
           {
               if (moveTo.Stopped)
               {
                   commandBuffer.RemoveComponent<IsMoving>(entityInQueryIndex, e);
                   mouvement.Velocity = new Velocity { Linear = float3.zero, Angular = float3.zero };
               }

           }).ScheduleParallel();

            Entities
            .WithChangeFilter<MoveTo>()
            .WithNone<IsDeadTag, IsMoving>()
            .ForEach((int entityInQueryIndex, Entity e, ref MoveTo moveTo, ref Mouvement mouvement) =>
            {
                if (!moveTo.Stopped)
                {
                    commandBuffer.AddComponent<IsMoving>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();
            // // Put velocity at zero if stopped
            // Entities
            // .WithNone<IsDeadTag, IsMoving>()
            // .WithChangeFilter<MoveTo>()
            // .ForEach((Entity e, ref Mouvement mouvement, in MoveTo moveTo) =>
            // {

            // }).ScheduleParallel();

            // Initialize move to when spawned
            Entities
            .WithAll<Spawned>()
            .ForEach((ref Translation position, ref MoveTo moveTo) =>
            {
                moveTo.Position = position.Value;
            }).ScheduleParallel();

            Entities
            .WithAll<WarpTo>()
            .ForEach((int entityInQueryIndex, Entity e) =>
            {
                commandBuffer.RemoveComponent<WarpTo>(entityInQueryIndex, e);
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

        }
    }
    [UpdateInGroup(typeof(MouvementSystemGroup))]
    [UpdateAfter(typeof(MoveToSystem))]
    public class MoveToNavMeshAgentSystem : SystemBase
    {

        EntityQuery navMeshAgentQueries;
        protected override void OnCreate()
        {
            base.OnCreate();
        }
        protected override void OnUpdate()
        {

            // TODO : Refractor get in paralle
            var lookAts = GetComponentDataFromEntity<LookAt>(true);

            // Warp when spawned or warp to
            Entities.WithoutBurst()
            .WithAny<Spawned, WarpTo>()
            .ForEach((NavMeshAgent agent, ref MoveTo moveTo, in Translation position) =>
            {
                if (agent.isOnNavMesh)
                {
                    agent.Warp(position.Value);
                    moveTo.Position = position.Value;
                }

            }).Run();

            Entities
            .WithAny<IsMoving>()
            .WithNone<IsDeadTag, WarpTo>()
            .WithChangeFilter<MoveTo>()
            .WithReadOnly(lookAts)
            .WithStoreEntityQueryInField(ref navMeshAgentQueries)
            .ForEach((Entity e, NavMeshAgent agent, ref Translation position, ref Mouvement mouvement, ref MoveTo moveTo, ref Rotation rotation) =>
            {
                if (agent.isOnNavMesh)
                {
                    agent.speed = moveTo.CalculeSpeed(in mouvement);
                    agent.SetDestination(moveTo.Position);
                    position.Value = agent.transform.position;
                    mouvement.Velocity = new Velocity
                    {
                        Linear = agent.transform.InverseTransformDirection(agent.velocity),
                        Angular = agent.angularSpeed

                    };
                    if (!lookAts.HasComponent(e) || lookAts[e].Entity == Entity.Null)
                    {
                        rotation.Value = agent.transform.rotation;
                    }
                }

            }).WithoutBurst().Run();

        }
    }

}
