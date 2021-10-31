using Unity.Entities;
using UnityEngine;
using Unity.Transforms;

namespace RPG.Core
{
    public struct HasHybridComponent : IComponentData
    {

    }
    public struct Spawn : IComponentData
    {
        public Entity Prefab;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    public class SpawnSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            var commandBufferP = commandBuffer.AsParallelWriter();
            Entities.WithNone<HasHybridComponent>().ForEach((int entityInQueryIndex, Entity e, in Spawn toSpawn, in LocalToWorld localToWorld) =>
               {
                   var instance = commandBufferP.Instantiate(entityInQueryIndex, toSpawn.Prefab);
                   commandBufferP.AddComponent<Translation>(entityInQueryIndex, instance, new Translation { Value = localToWorld.Position });
                   commandBufferP.AddComponent<Rotation>(entityInQueryIndex, instance, new Rotation { Value = localToWorld.Rotation });
                   commandBufferP.RemoveComponent<Spawn>(entityInQueryIndex, e);
               }
            ).ScheduleParallel();
            var em = EntityManager;
            Entities.WithAny<HasHybridComponent>().WithStructuralChanges().ForEach((Entity e, in Spawn toSpawn, in LocalToWorld localToWorld) =>
                {
                    var instance = em.Instantiate(toSpawn.Prefab);
                    commandBuffer.AddComponent<Translation>(instance, new Translation { Value = localToWorld.Position });
                    commandBuffer.AddComponent<Rotation>(instance, new Rotation { Value = localToWorld.Rotation });
                    commandBuffer.RemoveComponent<Spawn>(e);
                }
            ).Run();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
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
                DstEntityManager.AddComponentData(entity, new LocalToWorld { Value = spawner.transform.localToWorldMatrix });
                if (spawner.HasHybridComponent)
                {
                    DstEntityManager.AddComponent<HasHybridComponent>(entity);
                }
            });
        }
    }
}
