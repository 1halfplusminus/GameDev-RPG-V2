using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using Unity.Mathematics;

namespace RPG.Mouvement
{
    public class MouvementSystemGroup : ComponentSystemGroup
    {

    }
    [UpdateInGroup(typeof(MouvementSystemGroup))]
    public class StopAtDistanceSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithChangeFilter<MoveTo, LocalToWorld>().ForEach((ref MoveTo moveTo, in LocalToWorld localToWorld) =>
             {
                 moveTo.Distance = math.distance(moveTo.Position, localToWorld.Position);
                 if (moveTo.Distance <= moveTo.StoppingDistance)
                 {
                     moveTo.Position = localToWorld.Position;
                 }
             }).ScheduleParallel();
        }
    }
    [UpdateInGroup(typeof(MouvementSystemGroup))]
    [UpdateAfter(typeof(StopAtDistanceSystem))]
    public class MoveToNavMeshAgentSystem : SystemBase
    {
        EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

        EntityQuery navMeshAgentQueries;
        protected override void OnCreate()
        {
            base.OnCreate();
            endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            Entities
            .WithStoreEntityQueryInField(ref navMeshAgentQueries)
            .WithoutBurst()
            .WithAll<Mouvement>()
            .ForEach((NavMeshAgent agent, ref Translation position, ref Mouvement mouvement, ref MoveTo moveTo, ref Rotation rotation) =>
            {
                if (agent.isOnNavMesh)
                {

                    agent.SetDestination(moveTo.Position);
                    position.Value = agent.transform.position;
                    rotation.Value = agent.transform.rotation;
                    mouvement.Velocity = new Velocity { Linear = agent.transform.InverseTransformDirection(agent.velocity), Angular = agent.angularSpeed };
                }
            }).Run();
            // TODO: Put in another system
            Entities.ForEach((int entityInQueryIndex, Entity e, in MoveTo moveTo, in LocalToWorld localToWorld) =>
            {
                if (math.distance(moveTo.Position, localToWorld.Position) >= moveTo.StoppingDistance)
                {
                    /* commandBuffer.RemoveComponent<MoveTo>(entityInQueryIndex, e);
                    Debug.Log("Arrive at destination"); */
                }
            }).Schedule();
            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }
    }

}
