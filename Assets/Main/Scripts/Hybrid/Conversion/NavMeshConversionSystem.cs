using UnityEngine.AI;
using RPG.Mouvement;
using Unity.AI.Navigation;
using Unity.Entities;
using UnityEngine;

public struct InitializedSurface : IComponentData
{

}
public class NavMeshAgentConversionSystem : GameObjectConversionSystem
{
    protected override void OnCreate()
    {
        base.OnCreate();
        this.AddTypeToCompanionWhiteList(typeof(MeshFilter));
        this.AddTypeToCompanionWhiteList(typeof(NavMeshSurface));
        this.AddTypeToCompanionWhiteList(typeof(NavMeshAgent));
        this.AddTypeToCompanionWhiteList(typeof(NavMeshObstacle));
    }
    protected override void OnUpdate()
    {
        Entities.ForEach((NavMeshSurface surface) =>
       {
           var entity = GetPrimaryEntity(surface);
           DstEntityManager.AddComponentObject(entity, surface);
           DstEntityManager.AddComponentObject(entity, surface.GetComponent<MeshFilter>());
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
            DstEntityManager.AddComponentObject(entity, agent);
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
        .ForEach((Entity e, NavMeshSurface surface) =>
        {
            Debug.Log("Init Nav Mesh Surface");
            NavMesh.AddNavMeshData(surface.navMeshData);
            // NavMeshSurface.activeSurfaces.Add(surface);
            // surface.gameObject.SetActive(true);
            // surface.enabled = false;
            // surface.enabled = true;
            // surface.gameObject.hideFlags = HideFlags.None;
            // surface.AddData();
            cb.AddComponent<InitializedSurface>(e);
        }).WithoutBurst().Run();
        entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}