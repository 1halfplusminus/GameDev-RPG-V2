using UnityEngine.AI;
using RPG.Mouvement;
using Unity.Animation;
using Unity.AI.Navigation;
using Unity.Entities;
using Unity.Transforms;

[DisableAutoCreation]
[UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
public class NavMeshAgentReferenceAssetConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((NavMeshSurface surface) =>
        {
            DeclareReferencedAsset(surface.navMeshData);
        });
    }
}

public class NavMeshAgentConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((NavMeshSurface surface) =>
       {
           var entity = GetPrimaryEntity(surface);
           AddHybridComponent(surface);
           if (surface.navMeshData)
           {
               DeclareAssetDependency(surface.gameObject, surface.navMeshData);
           }
       });
        Entities.ForEach((NavMeshObstacle obstacle) =>
        {
            AddHybridComponent(obstacle);
        });
        Entities.ForEach((NavMeshAgent agent) =>
        {
            var entity = GetPrimaryEntity(agent);
            AddHybridComponent(agent);
            DstEntityManager.AddComponentData(entity, new Mouvement { Speed = agent.speed });
            DstEntityManager.AddComponentData(entity, new MoveTo(agent.transform.position) { StoppingDistance = agent.stoppingDistance });
        });
    }
}
