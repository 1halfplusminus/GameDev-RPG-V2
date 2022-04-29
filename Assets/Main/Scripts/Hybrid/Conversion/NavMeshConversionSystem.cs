using UnityEngine.AI;
using RPG.Mouvement;
using Unity.AI.Navigation;
using Unity.Entities;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

public struct NavMeshAgentComponent
{
    public float StoppingDistance;

}
public struct InitializedSurface : ISystemStateComponentData
{
    public NavMeshDataInstance NavMeshDataInstance;
}
public class NavMeshAgentConversionSystem : GameObjectConversionSystem
{
    protected override void OnCreate()
    {
        base.OnCreate();
        // this.AddTypeToCompanionWhiteList(typeof(NavMeshSurface));
        // this.AddTypeToCompanionWhiteList(typeof(NavMeshAgent));
        this.AddTypeToCompanionWhiteList(typeof(NavMeshObstacle));
    }
    protected override void OnUpdate()
    {
        Entities.ForEach((NavMeshSurface surface) =>
       {
           var entity = GetPrimaryEntity(surface);
           DstEntityManager.AddComponentObject(entity, surface.navMeshData);
           if (surface.navMeshData)
           {
               DeclareAssetDependency(surface.gameObject, surface.navMeshData);
           }
       });
        Entities.ForEach((NavMeshObstacle obstacle) =>
        {
            var entity = GetPrimaryEntity(obstacle);
            DstEntityManager.AddComponentObject(entity, obstacle);
        });
        Entities.ForEach((NavMeshAgent agent) =>
        {
            var entity = GetPrimaryEntity(agent);
            // DstEntityManager.AddComponentObject(entity, agent);
            // DstEntityManager.AddComponentData(entity, new NavMeshAgentComponent { StoppingDistance = agent. });
            DstEntityManager.AddComponentData(entity, new Mouvement { Speed = agent.speed });
            DstEntityManager.AddComponentData(entity, new MoveTo(agent.transform.position) { StoppingDistance = agent.stoppingDistance });
        });
    }
}
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class InitialiseNavMeshSystem : SystemBase
{
    EntityCommandBufferSystem entityCommandBufferSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var cb = entityCommandBufferSystem.CreateCommandBuffer();
        Entities
        .WithNone<InitializedSurface>()
        .ForEach((Entity e, NavMeshData data) =>
        {
            Debug.Log("Init Nav Mesh Surface");
            var dataInstance = NavMesh.AddNavMeshData(data);
            cb.AddComponent<InitializedSurface>(e, new InitializedSurface { NavMeshDataInstance = dataInstance });
        })
        .WithoutBurst()
        .Run();

        Entities
        .WithNone<NavMeshData>()
        .ForEach((Entity e, InitializedSurface surface) =>
        {
            Debug.Log("Clean up surface");
            NavMesh.RemoveNavMeshData(surface.NavMeshDataInstance);
            cb.RemoveComponent<InitializedSurface>(e);
        }).Run();
        entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}