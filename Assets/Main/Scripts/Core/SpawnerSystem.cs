using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using System.Collections.Generic;
using Unity.Collections;

namespace RPG.Core
{
    public class SceneGUIDAuthoring : MonoBehaviour
    {
        public Unity.Entities.Hash128 SceneGUID;

        public int Section;
        public Entity SceneEntity;

        public Entity Spawner;

        public Unity.Entities.Hash128 Id;

    }
    public struct DestroySpawn : IComponentData
    {

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
        public Entity Parent;
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

                    Debug.Log("Convert Spawned Entity");
                    DstEntityManager.AddSharedComponentData(entity, new SceneSection { SceneGUID = sceneGUIDAuthoring.SceneGUID, Section = sceneGUIDAuthoring.Section });
                    DstEntityManager.AddSharedComponentData(entity, new SceneTag() { SceneEntity = sceneGUIDAuthoring.SceneEntity });
                    DstEntityManager.AddComponentData(entity, new GameObjectSpawn() { SpawnerEntity = sceneGUIDAuthoring.Spawner });
                    DstEntityManager.AddComponentData(entity, new Spawned() { });
                    AddHybridComponent(sceneGUIDAuthoring);
                }
            });
        }
    }

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
                if (!entities.HasComponent(gameObjectSpawn.SpawnerEntity))
                {
                    Debug.Log("But spawner doesn't");
                    cmp.AddComponent<GameObjectSpawnDestroy>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();

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

        struct SceneInfo
        {
            public Unity.Entities.Hash128 SceneGUID;
            public int Section;
            public Entity SceneEntity;
        }
        EntityCommandBufferSystem entityCommandBufferSystem;

        GameObject rootGameObjectSpawner;

        EntityQuery spawnerSceneTagQuery;

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
        protected void AddSceneGUIDRecurse(GameObject gameObject, Entity sceneEntity, Entity spawnerEntity, Unity.Entities.Hash128 sceneGUID, int section, UnityEngine.Hash128 hash = default)
        {
            var component = gameObject.AddComponent<SceneGUIDAuthoring>();
            component.SceneGUID = sceneGUID;
            component.Spawner = spawnerEntity;
            component.SceneEntity = sceneEntity;
            component.Section = section;
            foreach (Transform child in gameObject.transform)
            {
                AddSceneGUIDRecurse(child.gameObject, sceneEntity, spawnerEntity, sceneGUID, section, hash);
            }
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            var commandBufferP = commandBuffer.AsParallelWriter();
            var em = EntityManager;
            var sceneComponents = new NativeHashMap<Entity, SceneInfo>(spawnerSceneTagQuery.CalculateEntityCount(), Allocator.TempJob);

            Entities
            .WithAll<Spawn>()
            .WithNone<HasSpawn>()
            .WithStoreEntityQueryInField(ref spawnerSceneTagQuery).ForEach((Entity e, SceneTag sceneTag, SceneSection section) =>
            {
                sceneComponents.Add(e, new SceneInfo() { SceneEntity = sceneTag.SceneEntity, SceneGUID = section.SceneGUID, Section = section.Section });
            })
            .WithoutBurst().Run();
            sceneComponents.Dispose();
            Entities
            .WithNone<DestroySpawn>()
            .WithNone<HasHybridComponent, GameObject, GameObjectSpawner>().ForEach((int entityInQueryIndex, Entity e, in Spawn toSpawn, in LocalToWorld localToWorld) =>
                  {

                      var instance = commandBufferP.Instantiate(entityInQueryIndex, toSpawn.Prefab);
                      if (toSpawn.Parent != Entity.Null)
                      {
                          commandBufferP.AddComponent(entityInQueryIndex, instance, new Parent { Value = toSpawn.Parent });
                          commandBufferP.AddComponent<LocalToParent>(entityInQueryIndex, instance);
                      }
                      if (toSpawn.Parent != e)
                      {
                          commandBufferP.AddComponent(entityInQueryIndex, instance, new Translation { Value = localToWorld.Position });
                          commandBufferP.AddComponent(entityInQueryIndex, instance, new Rotation { Value = localToWorld.Rotation });
                      }

                      commandBufferP.AddComponent<Spawned>(entityInQueryIndex, instance);

                      commandBufferP.AddComponent(entityInQueryIndex, e, new HasSpawn { Entity = instance });
                      commandBufferP.RemoveComponent<Spawn>(entityInQueryIndex, e);

                  }
            ).ScheduleParallel();
            /*  Entities
             .WithNone<DestroySpawn>()
             .WithReadOnly(sceneComponents)
             .WithDisposeOnCompletion(sceneComponents)
             .WithNone<HasHybridComponent, GameObject, GameObjectSpawner>().ForEach((int entityInQueryIndex, Entity e, in HasSpawn hasSpawn) =>
                  {

                      if (sceneComponents.ContainsKey(e))
                      {
                          commandBuffer.AddSharedComponent(hasSpawn.Entity, new SceneTag() { SceneEntity = sceneComponents[e].SceneEntity });
                          commandBuffer.AddSharedComponent(hasSpawn.Entity, new SceneSection() { SceneGUID = sceneComponents[e].SceneGUID, Section = sceneComponents[e].Section });
                      }

                  }
             ).WithStructuralChanges().Run(); */
            Entities
            .WithNone<GameObject, GameObjectSpawner, DestroySpawn>()
            .WithAny<HasHybridComponent>()
            .WithStructuralChanges()
            .ForEach((Entity e, in Spawn toSpawn, in LocalToWorld localToWorld) =>
                {
                    var instance = em.Instantiate(toSpawn.Prefab);
                    commandBuffer.AddComponent(instance, new Translation
                    {
                        Value = localToWorld.Position
                    });
                    commandBuffer.AddComponent(instance, new Rotation { Value = localToWorld.Rotation });
                    commandBuffer.AddComponent<Spawned>(instance);

                    commandBuffer.AddComponent(e, new HasSpawn { Entity = instance });
                    commandBuffer.RemoveComponent<Spawn>(e);
                }
            ).Run();

            Entities
            .WithNone<DestroySpawn>()
            .WithChangeFilter<Spawn>()
            .WithStructuralChanges()
            .WithAny<GameObjectSpawner>()
            .WithoutBurst()
            .ForEach((Entity e, GameObject prefab, in Spawn toSpawn, in LocalToWorld localToWorld, in SceneTag sceneTag, in SceneSection section) =>
            {
                if (prefab != null)
                {
                    var instance = Object.Instantiate(prefab);
                    AddSceneGUIDRecurse(instance, sceneTag.SceneEntity, e, section.SceneGUID, section.Section);
                }
            }).Run();
            Entities.
             WithAny<Spawned>()
            .ForEach((Entity e, SceneGUIDAuthoring sceneGUIDAuthoring) =>
                 {
                     if (sceneGUIDAuthoring.transform.parent == null)
                     {
                         sceneGUIDAuthoring.transform.parent = rootGameObjectSpawner.transform;
                         if (sceneGUIDAuthoring.Spawner != Entity.Null)
                         {
                             commandBuffer.RemoveComponent<Spawn>(sceneGUIDAuthoring.Spawner);
                             commandBuffer.AddComponent(sceneGUIDAuthoring.Spawner, new HasSpawn { Entity = e });
                         }
                     }
                 }
            ).WithoutBurst().Run();
            Entities.WithAny<Spawned>().ForEach((int entityInQueryIndex, Entity e) =>
                {
                    commandBufferP.RemoveComponent<Spawned>(entityInQueryIndex, e);
                }
            ).ScheduleParallel();
            Entities.WithAny<DestroySpawn>().ForEach((int entityInQueryIndex, Entity e, ref HasSpawn spawn) =>
                {
                    if (spawn.Entity != Entity.Null)
                    {
                        Debug.Log($"Destroy Spawn for {spawn.Entity}");
                        commandBufferP.DestroyEntity(entityInQueryIndex, spawn.Entity);
                        spawn.Entity = Entity.Null;
                    }
                }
            ).ScheduleParallel();
            Entities.WithAny<DestroySpawn>().ForEach((int entityInQueryIndex, Entity e) =>
              {
                  commandBufferP.RemoveComponent<DestroySpawn>(entityInQueryIndex, e);
              }
            ).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }


}
