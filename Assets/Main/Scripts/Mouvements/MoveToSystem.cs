using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using Unity.Mathematics;
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
       .ForEach(( NavMeshAgent agent, ref Translation position,ref Mouvement mouvement, ref  MoveTo moveTo,ref Rotation rotation)=>{
           if(agent.isOnNavMesh) {
                agent.SetDestination( moveTo.Position);
                position.Value = agent.transform.position; 
                rotation.Value = agent.transform.rotation;
                Debug.Log("Moving toward: " + agent.destination);
                moveTo.StoppingDistance = agent.stoppingDistance;
                mouvement.Velocity = new Velocity{Linear = agent.transform.InverseTransformDirection(agent.velocity), Angular = agent.angularSpeed};
          }
       }).Run();
       // TODO: Put in another system
       Entities.ForEach((int entityInQueryIndex,Entity e, in MoveTo moveTo, in LocalToWorld localToWorld)=> {
           if(math.distance(moveTo.Position, localToWorld.Position) >= moveTo.StoppingDistance) {
               /* commandBuffer.RemoveComponent<MoveTo>(entityInQueryIndex, e);
               Debug.Log("Arrive at destination"); */
           }
       }).Schedule();
       endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}
