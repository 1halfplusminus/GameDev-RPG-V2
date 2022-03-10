using Unity.Entities;
using UnityEngine;
using Unity.Scenes;
using Unity.Collections;
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
    public struct TriggeredSceneLoaded : IComponentData
    {
        public Entity SceneEntity;
        public Unity.Entities.Hash128 SceneGUID;
    }
    public struct SceneLoaded : IComponentData
    {
        public Unity.Entities.Hash128 SceneGUID;
    }
    public struct LoadSceneAsync : IComponentData
    {
        public Entity SceneEntity;

        public Unity.Entities.Hash128 SceneGUID;
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
    [UpdateAfter(typeof(SpawnSystem))]
    public class SceneLoadingSystem : SystemBase
    {
        SceneSystem sceneSystem;

        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery sceneLoadingQuery;

        EntityQuery sceneLoadedQuery;

        EntityQuery waitForSpawn;

        EntityQuery anySceneFinishLoadingQuery;
        public static void UnloadAllCurrentlyLoadedScene(EntityManager dstManager)
        {
            if (dstManager.World.Flags == WorldFlags.Game)
            {
                var loadedSceneQuery = dstManager.CreateEntityQuery(typeof(SceneLoaded));
                using var currentLoadedScene = loadedSceneQuery.ToComponentDataArray<SceneLoaded>(Allocator.Temp);
                using var loadedScenes = loadedSceneQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < currentLoadedScene.Length; i++)
                {
                    dstManager.AddComponentData(loadedScenes[i], new UnloadScene() { SceneEntity = loadedScenes[i] });
                }
            }
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            sceneSystem = World.GetOrCreateSystem<SceneSystem>();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            sceneLoadedQuery = GetEntityQuery(ComponentType.ReadOnly(typeof(TriggeredSceneLoaded)));
            waitForSpawn = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(Spawn), typeof(SceneTag) },
                None = new ComponentType[] { typeof(HasSpawn) }
            });
            sceneLoadingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(LoadSceneAsync) }
            });
            anySceneFinishLoadingQuery = GetEntityQuery(typeof(AnySceneFinishLoading));
        }
        protected override void OnUpdate()
        {
            EntityManager.RemoveComponent<TriggeredSceneLoaded>(sceneLoadedQuery);
            var _sceneSystem = sceneSystem;
            var em = EntityManager;
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            var commandBufferP = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .ForEach((Entity e, in TriggerSceneLoad triggerSceneLoad) =>
            {
                var sceneEntity = _sceneSystem.GetSceneEntity(triggerSceneLoad.SceneGUID);
                if (sceneEntity == Entity.Null || !_sceneSystem.IsSceneLoaded(sceneEntity))
                {
                    var loadParameters = new SceneSystem.LoadParameters { Flags = SceneLoadFlags.LoadAdditive };
                    sceneEntity = _sceneSystem.LoadSceneAsync(triggerSceneLoad.SceneGUID, loadParameters);
                    var newSceneRef = em.GetComponentData<SceneReference>(sceneEntity);
                    Debug.Log($"Loading Scene {newSceneRef.SceneGUID}");
                    commandBuffer.AddComponent(e, new LoadSceneAsync() { SceneEntity = sceneEntity, SceneGUID = newSceneRef.SceneGUID });
                    commandBuffer.RemoveComponent<TriggerSceneLoad>(e);
                }
            })
            .WithStructuralChanges()
            .Run();

            Entities.ForEach((Entity e, in TriggerUnloadScene unloadScene) =>
            {
                Debug.Log($"Unload Scene {unloadScene.SceneGUID}");
                var sceneEntity = sceneSystem.GetSceneEntity(unloadScene.SceneGUID);
                commandBuffer.AddComponent(e, new UnloadScene() { SceneEntity = sceneEntity });
                commandBuffer.RemoveComponent<TriggerUnloadScene>(e);
            })
            .WithoutBurst()
            .Run();

            Entities.ForEach((Entity e, in UnloadScene unloadScene) =>
            {
                Debug.Log($"Unload Scene {unloadScene.SceneEntity}");
                _sceneSystem.UnloadScene(unloadScene.SceneEntity);
                commandBuffer.RemoveComponent<SceneLoaded>(unloadScene.SceneEntity);
                commandBuffer.RemoveComponent<UnloadScene>(e);
            })
            .WithStructuralChanges()
            .Run();
            // Delete scene finish loading notification
            commandBuffer.RemoveComponentForEntityQuery<AnySceneFinishLoading>(anySceneFinishLoadingQuery);

            if (sceneLoadingQuery.IsEmpty)
            {
                Entities
                .ForEach((int entityInQueryIndex, Entity e, in AnySceneLoading anySceneLoading) =>
                {
                    commandBufferP.RemoveComponent<AnySceneLoading>(entityInQueryIndex, e);
                    commandBufferP.AddComponent(entityInQueryIndex, e, new AnySceneFinishLoading { Triggers = anySceneLoading.Triggers });
                }).ScheduleParallel();
            }
            else
            {
                // Check if any scene is loading
                var sceneLoadingCount = sceneLoadingQuery.CalculateEntityCount();
                // var nativeListTrigger = new NativeList<Entity>(sceneLoadingCount, Allocator.TempJob);
                // var nativeListTriggerWriter = nativeListTrigger.AsParallelWriter();
                // Entities
                // .WithStoreEntityQueryInField(ref sceneLoadingQuery)
                // .ForEach((Entity e, in LoadSceneAsync loadSceneAsync) =>
                // {
                //     // nativeListTriggerWriter.AddNoResize(e);
                // }).ScheduleParallel();

                // Entities
                // .WithReadOnly(nativeListTrigger)
                // .WithDisposeOnCompletion(nativeListTrigger)
                // .WithAny<SceneLoadingListener>()
                // .ForEach((int entityInQueryIndex, Entity e) =>
                // {
                //     var triggers = new FixedList128<Entity>();
                //     foreach (var trigger in nativeListTrigger)
                //     {
                //         triggers.Add(trigger);
                //     }
                //     commandBufferP.AddComponent(entityInQueryIndex, e, new AnySceneLoading { Triggers = triggers });
                // })
                // .ScheduleParallel();
                CheckIfSceneFinishLoading(sceneLoadingCount, out NativeArray<Entity> loadingScenes, out NativeArray<LoadSceneAsync> loadingScenesData, out NativeHashMap<Entity, TriggeredSceneLoaded> sceneLoadedList);
                loadingScenes.Dispose();
                loadingScenesData.Dispose();
                sceneLoadedList.Dispose();
            }

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        private void CheckIfSceneFinishLoading(int sceneLoadingCount, out NativeArray<Entity> loadingScenes, out NativeArray<LoadSceneAsync> loadingScenesData, out NativeHashMap<Entity, TriggeredSceneLoaded> sceneLoadedList)
        {
            loadingScenes = sceneLoadingQuery.ToEntityArray(Allocator.Temp);
            loadingScenesData = sceneLoadingQuery.ToComponentDataArray<LoadSceneAsync>(Allocator.Temp);
            sceneLoadedList = new NativeHashMap<Entity, TriggeredSceneLoaded>(sceneLoadingCount, Allocator.Temp);
            for (int i = 0; i < loadingScenes.Length; i++)
            {
                if (sceneSystem.IsSceneLoaded(loadingScenesData[i].SceneEntity))
                {
                    var allSectionLoaded = false;
                    var resolvedSections = EntityManager.GetBuffer<ResolvedSectionEntity>(loadingScenesData[i].SceneEntity);
                    for (int j = 0; j < resolvedSections.Length; j++)
                    {
                        waitForSpawn.SetSharedComponentFilter(new SceneTag() { SceneEntity = resolvedSections[j].SectionEntity });
                        var sectionLoaded = sceneSystem.IsSectionLoaded(resolvedSections[j].SectionEntity) && waitForSpawn.CalculateEntityCount() == 0;
                        allSectionLoaded = j == 0 ? sectionLoaded : allSectionLoaded && sectionLoaded;
                        waitForSpawn.ResetFilter();
                        if (!sectionLoaded)
                        {
                            break;
                        }
                    }
                    if (allSectionLoaded)
                    {
                        Debug.Log($"Scene {loadingScenesData[i].SceneGUID} is loaded");
                        EntityManager.AddComponentData(loadingScenesData[i].SceneEntity, new SceneLoaded { SceneGUID = loadingScenesData[i].SceneGUID });
                        sceneLoadedList.Add(loadingScenes[i], new TriggeredSceneLoaded { SceneGUID = loadingScenesData[i].SceneGUID });
                        EntityManager.AddComponentData(loadingScenes[i], new TriggeredSceneLoaded { SceneGUID = loadingScenesData[i].SceneGUID });
                    }
                    else
                    {
                        // Debug.Log($"Scene section for {loadingScenesData[i].SceneGUID} is still loading");
                    }
                }
                else
                {
                    // Debug.Log($"Scene {loadingScenesData[i].SceneGUID} is still loading");
                }
            }
            var sceneLoaded = sceneLoadedList.GetKeyArray(Allocator.Temp);
            EntityManager.RemoveComponent<LoadSceneAsync>(sceneLoaded);
            sceneLoaded.Dispose();
        }
    }
}
