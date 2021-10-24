
using UnityEngine.AI;
using Unity.Physics;
public class NavMeshAgentConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((NavMeshObstacle obstacle) =>
        {
            AddHybridComponent(obstacle);
        });
        Entities.ForEach((NavMeshAgent agent) =>
        {
            AddHybridComponent(agent);
            var entity = GetPrimaryEntity(agent);
            DstEntityManager.AddComponent<Mouvement>(entity);
        });
    }
}
