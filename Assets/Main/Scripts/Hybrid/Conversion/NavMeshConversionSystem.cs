using UnityEngine.AI;
using RPG.Mouvement;
using Unity.AI.Navigation;
using Unity.Entities;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using System;

public struct NavMeshAgentComponent : IComponentData
{

}

public struct InitializedSurface : ISystemStateComponentData
{
    public NavMeshDataInstance NavMeshDataInstance;
}
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class NavMeshInitializationSystem : SystemBase
{
    EntityCommandBufferSystem entityCommandBufferSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        Entities
        .WithNone<ManagedPath>()
        .WithAll<NavMeshAgentComponent>()
        .ForEach((Entity e) =>
        {

            var path = new NavMeshPath();
            var managedPath = new ManagedPath();
            managedPath.Path = path;
            commandBuffer.AddComponent(e, managedPath);
        }).WithoutBurst().Run();

        // Entities.ForEach((ref PathComponent pathComponent) =>
        // {
        //     unsafe
        //     {
        //         var handle = GCHandle.FromIntPtr(pathComponent.Ptr);
        //         var path = (NavMeshPath)handle.Target;
        //         NavMesh.CalculatePath(Vector3.up, Vector3.down, -1, path);
        //     }
        // }).WithoutBurst().Schedule();
    }
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
            DstEntityManager.AddComponent<NavMeshAgentComponent>(entity);
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
            if (data != null)
            {
                var dataInstance = NavMesh.AddNavMeshData(data);
                cb.AddComponent<InitializedSurface>(e, new InitializedSurface { NavMeshDataInstance = dataInstance });
            }
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