using Unity.Entities;
using UnityEngine;


namespace RPG.Core
{
    public struct Spawn : IComponentData
    {
        public Entity Prefab;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    public class SpawnSystem : SystemBase
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
            Entities.ForEach((Entity e, in Spawn toSpawn) =>
               {
                   commandBuffer.Instantiate(toSpawn.Prefab);
                   commandBuffer.RemoveComponent<Spawn>(e);
               }
            ).ScheduleParallel();
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
    public class SpawnConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PlayerSpawner spawner) =>
            {
                var prefabEntity = GetPrimaryEntity(spawner.Prefab);
                var entity = GetPrimaryEntity(spawner);
                DstEntityManager.AddComponentData(entity, new Spawn { Prefab = prefabEntity });
            });
        }
    }
}
