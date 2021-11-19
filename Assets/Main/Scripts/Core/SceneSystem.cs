using Unity.Entities;
using UnityEngine;
using RPG.Control;
using Unity.Scenes;
using RPG.Mouvement;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Gameplay;

namespace RPG.Core
{

    public struct TriggerSceneLoad : IComponentData
    {
        public Unity.Entities.Hash128 SceneGUID;
    }
    public struct TriggerUnloadScene : IComponentData
    {
        public Unity.Entities.Hash128 SceneGUID;
    }
    public struct LoadingScene : IComponentData
    {

    }
    public struct LoadSceneAsync : IComponentData
    {
        public Entity SceneEntity;
    }
    public struct UnloadScene : IComponentData
    {
        public Entity SceneEntity;
    }

    public struct AnySceneLoading : IComponentData
    {
        public FixedList128<Entity> Triggers;
    }
    public struct AnySceneFinishLoading : IComponentData
    {
        public FixedList128<Entity> Triggers;
    }

    [UpdateInGroup(typeof(CoreSystemGroup))]

    public class SceneLoadingSystem : SystemBase
    {
        SceneSystem sceneSystem;


        EntityCommandBufferSystem entityCommandBufferSystem;



        EntityQuery sceneLoadingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            sceneSystem = World.GetOrCreateSystem<SceneSystem>();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var _sceneSystem = sceneSystem;
            var em = EntityManager;
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            var commandBufferP = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .ForEach((Entity e, in TriggerSceneLoad triggerSceneLoad) =>
            {
                var sceneEntity = _sceneSystem.LoadSceneAsync(triggerSceneLoad.SceneGUID);
                var newSceneRef = em.GetComponentData<SceneReference>(sceneEntity);
                Debug.Log("Loaded Scene " + newSceneRef.SceneGUID);
                commandBuffer.AddComponent(e, new LoadSceneAsync() { SceneEntity = sceneEntity });
                commandBuffer.RemoveComponent<TriggerSceneLoad>(e);
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            Entities.ForEach((Entity e, in TriggerUnloadScene unloadScene) =>
            {
                Debug.Log("Unload Scene" + unloadScene.SceneGUID);
                _sceneSystem.UnloadScene(sceneSystem.GetSceneEntity(unloadScene.SceneGUID));
                commandBuffer.RemoveComponent<TriggerUnloadScene>(e);
            }).WithStructuralChanges().WithoutBurst().Run();

            // Delete scene finish loading notification
            Entities
            .WithAny<AnySceneFinishLoading>()
            .ForEach((int entityInQueryIndex, Entity e) =>
            {
                commandBufferP.RemoveComponent<AnySceneFinishLoading>(entityInQueryIndex, e);
            }).ScheduleParallel();

            if (sceneLoadingQuery.IsEmpty)
            {
                Entities
                .ForEach((int entityInQueryIndex, Entity e, in AnySceneLoading anySceneLoading) =>
                {
                    commandBufferP.RemoveComponent<AnySceneLoading>(entityInQueryIndex, e);
                    commandBufferP.AddComponent<AnySceneFinishLoading>(entityInQueryIndex, e, new AnySceneFinishLoading { Triggers = anySceneLoading.Triggers });
                }).ScheduleParallel();
            }
            else
            {
                var nativeListTrigger = new NativeList<Entity>(sceneLoadingQuery.CalculateEntityCount(), Allocator.TempJob);
                var nativeListTriggerWriter = nativeListTrigger.AsParallelWriter();
                Entities
                .WithStoreEntityQueryInField(ref sceneLoadingQuery)
                .ForEach((Entity e, in LoadSceneAsync loadSceneAsync) =>
                {
                    nativeListTriggerWriter.AddNoResize(e);
                }).ScheduleParallel();

                Entities
                .WithReadOnly(nativeListTrigger)
                .WithDisposeOnCompletion(nativeListTrigger)
                .WithAny<SceneLoadingListener>()
                .ForEach((int entityInQueryIndex, Entity e) =>
                {
                    var triggers = new FixedList128<Entity>();
                    foreach (var trigger in nativeListTrigger)
                    {
                        triggers.Add(trigger);
                    }
                    commandBufferP.AddComponent(entityInQueryIndex, e, new AnySceneLoading { Triggers = triggers });
                })
                .ScheduleParallel();
                using var loadingScenes = sceneLoadingQuery.ToEntityArray(Allocator.Temp);
                using var loadingScenesData = sceneLoadingQuery.ToComponentDataArray<LoadSceneAsync>(Allocator.Temp);
                using var sceneLoadedList = new NativeList<Entity>(Allocator.Temp);
                for (int i = 0; i < loadingScenes.Length; i++)
                {
                    if (sceneSystem.IsSceneLoaded(loadingScenesData[i].SceneEntity))
                    {
                        Debug.Log("Scene is loaded");
                        sceneLoadedList.Add(loadingScenes[i]);
                    }
                }
                var sceneLoaded = sceneLoadedList.ToArray(Allocator.Temp);
                EntityManager.RemoveComponent<LoadSceneAsync>(sceneLoaded);
                sceneLoaded.Dispose();


            }
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);


        }
    }
}

