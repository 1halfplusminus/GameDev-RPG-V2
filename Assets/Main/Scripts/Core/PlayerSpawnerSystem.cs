using Unity.Entities;
using UnityEngine;

public struct SpawnPlayer : IComponentData
{
    public Entity Prefab;
}

public class SpawnPlayerSystem : SystemBase
{
    EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        endSimulationEntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();
        Entities.WithoutBurst().ForEach((Entity e, SpawnPlayer toSpawn) =>
           {
               commandBuffer.Instantiate(toSpawn.Prefab);
               commandBuffer.RemoveComponent<SpawnPlayer>(e);
           }
        ).Run();
    }
}
[UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
public class SpawnDeclarePrefabsConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((PlayerSpawner spawner) =>
        {
            DeclareReferencedPrefab(spawner.Prefab);

        });
    }
}
public class SpawnPlayerConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((PlayerSpawner spawner) =>
        {
            var prefabEntity = GetPrimaryEntity(spawner.Prefab);
            var entity = GetPrimaryEntity(spawner);
            DstEntityManager.AddComponentData(entity, new SpawnPlayer { Prefab = prefabEntity });
        });
    }
}