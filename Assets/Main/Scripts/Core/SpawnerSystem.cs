using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Collections;
using System.Collections.Generic;

namespace RPG.Core
{
    public class SceneGUIDAuthoring : MonoBehaviour
    {
        public Entity SceneEntity;

        public Entity Spawner;

    }
    public struct GameObjectSpawner : IComponentData
    {

    }
    public struct GameObjectSpawnDestroy : ISystemStateComponentData
    {
        public Entity SpawnerEntity;

    }
    public struct GameObjectSpawn : ISystemStateComponentData
    {
        public Entity SpawnerEntity;

    }
    public struct GameObjectSpawnHandle : ISystemStateComponentData
    {

        public int Handle;
    }
    public struct HasHybridComponent : IComponentData
    {

    }
    public struct Spawn : IComponentData
    {
        public Entity Prefab;
    }

    public struct Spawned : IComponentData
    {

    }
    public struct HasSpawn : IComponentData
    {
        public Entity Entity;
    }

    public class SceneGUIDAuthoringConverionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((SceneGUIDAuthoring sceneGUIDAuthoring) =>
            {
                var entity = TryGetPrimaryEntity(sceneGUIDAuthoring);
                if (entity != Entity.Null)
                {
                    DstEntityManager.AddSharedComponentData(entity, new SceneTag() { SceneEntity = sceneGUIDAuthoring.SceneEntity });
                    DstEntityManager.AddComponentData(entity, new GameObjectSpawn() { SpawnerEntity = sceneGUIDAuthoring.Spawner });
                    DstEntityManager.AddComponentData(entity, new Spawned() { });
                    AddHybridComponent(sceneGUIDAuthoring);
                }
            });
        }
    }
    [DisableAutoCreation]
    public class GameObjectInstanciateSystem : SystemBase
    {


        EntityCommandBufferSystem entityCommandBufferSystem;

        Dictionary<int, GameObject> sceneGUIDs;


        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            sceneGUIDs = new Dictionary<int, GameObject>();

        }
        protected override void OnUpdate()
        {
            var em = EntityManager;
            var cm = entityCommandBufferSystem.CreateCommandBuffer();
            var cmp = cm.AsParallelWriter();
            var entities = GetComponentDataFromEntity<GameObjectSpawner>(true);
            Entities
            .WithReadOnly(entities)
            .WithNone<GameObjectSpawnDestroy>()
            .WithAll<GameObjectSpawnHandle>()
            .ForEach((int entityInQueryIndex, Entity e, in GameObjectSpawn gameObjectSpawn) =>
            {
                Debug.Log("I'm still here");
                if (!entities.HasComponent(gameObjectSpawn.SpawnerEntity))
                {
                    Debug.Log("But spawner doesn't");
                    /*  cmp.AddComponent<GameObjectSpawnDestroy>(entityInQueryIndex, e); */
                }
            }).Run();

            Entities
            .WithNone<GameObjectSpawnHandle>()
            .ForEach((Entity e, in SceneGUIDAuthoring sceneGUID) =>
            {
                Debug.Log("Add handle");
                sceneGUIDs.Add(e.Index, sceneGUID.gameObject);
                cm.AddComponent(e, new GameObjectSpawnHandle { Handle = e.Index });
            }).WithoutBurst().Run();



            Entities
            .WithAll<GameObjectSpawnDestroy>()
            .ForEach((Entity e, in GameObjectSpawn gameObjectSpawn, in GameObjectSpawnHandle handle) =>
            {
                Debug.Log("I need to be destroy");
                var go = sceneGUIDs[handle.Handle];
                Object.DestroyImmediate(go);
                cm.RemoveComponent<GameObjectSpawn>(e);
                cm.RemoveComponent<GameObjectSpawnHandle>(e);
                cm.RemoveComponent<GameObjectSpawnDestroy>(e);
                sceneGUIDs.Remove(handle.Handle);
                cm.DestroyEntity(e);
            }).WithoutBurst().Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }
    [UpdateInGroup(typeof(CoreSystemGroup))]

    public class SpawnSystem : SystemBase
    {

        EntityCommandBufferSystem entityCommandBufferSystem;

        GameObject rootGameObjectSpawner;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (rootGameObjectSpawner != null)
            {
                Object.DestroyImmediate(rootGameObjectSpawner);
            }
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            rootGameObjectSpawner = new GameObject("__SpawnerSystem__");

        }
        protected void AddSceneGUIDRecurse(GameObject gameObject, Entity sceneEntity, Entity spawnerEntity)
        {
            var component = gameObject.AddComponent<SceneGUIDAuthoring>();
            component.Spawner = spawnerEntity;
            component.SceneEntity = sceneEntity;
            foreach (Transform child in gameObject.transform)
            {
                AddSceneGUIDRecurse(child.gameObject, sceneEntity, spawnerEntity);
            }
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            var commandBufferP = commandBuffer.AsParallelWriter();
            var em = EntityManager;

            Entities.WithNone<HasHybridComponent, GameObject, GameObjectSpawner>().ForEach((int entityInQueryIndex, Entity e, in Spawn toSpawn, in LocalToWorld localToWorld) =>
                 {

                     var instance = commandBufferP.Instantiate(entityInQueryIndex, toSpawn.Prefab);
                     commandBufferP.AddComponent(entityInQueryIndex, instance, new Translation { Value = localToWorld.Position });
                     commandBufferP.AddComponent(entityInQueryIndex, instance, new Rotation { Value = localToWorld.Rotation });
                     commandBufferP.AddComponent<Spawned>(entityInQueryIndex, instance);

                     commandBufferP.AddComponent(entityInQueryIndex, e, new HasSpawn { Entity = instance });
                     commandBufferP.RemoveComponent<Spawn>(entityInQueryIndex, e);
                 }
            ).ScheduleParallel();

            Entities
            .WithNone<GameObject, GameObjectSpawner>()
            .WithAny<HasHybridComponent>()
            .WithStructuralChanges()
            .ForEach((Entity e, in Spawn toSpawn, in LocalToWorld localToWorld) =>
           {
               var instance = em.Instantiate(toSpawn.Prefab);
               commandBuffer.AddComponent(instance, new Translation { Value = localToWorld.Position });
               commandBuffer.AddComponent(instance, new Rotation { Value = localToWorld.Rotation });
               commandBuffer.AddComponent<Spawned>(instance);
               commandBuffer.RemoveComponent<Spawn>(e);
           }
            ).Run();
            Entities
            .WithStructuralChanges()
            .WithAny<GameObjectSpawner>()
            .WithoutBurst()
            .ForEach((Entity e, GameObject prefab, in Spawn toSpawn, in LocalToWorld localToWorld, in SceneTag sceneTag) =>
            {
                var instance = Object.Instantiate(prefab);
                AddSceneGUIDRecurse(instance, sceneTag.SceneEntity, e);

                var instancedEntity = em.CreateEntity();
                em.AddSharedComponentData(instancedEntity, sceneTag);
                em.AddComponentObject(instancedEntity, instance);
                commandBuffer.RemoveComponent<Spawn>(e);
            }).Run();
            Entities.
             WithAny<Spawned>()
            .ForEach((SceneGUIDAuthoring sceneGUIDAuthoring) =>
                 {
                     if (sceneGUIDAuthoring.transform.parent == null)
                     {
                         sceneGUIDAuthoring.transform.parent = rootGameObjectSpawner.transform;
                     }
                 }
            ).WithoutBurst().Run();
            Entities.WithAny<Spawned>().ForEach((int entityInQueryIndex, Entity e) =>
                {
                    commandBufferP.RemoveComponent<Spawned>(entityInQueryIndex, e);
                }
            ).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }


}
