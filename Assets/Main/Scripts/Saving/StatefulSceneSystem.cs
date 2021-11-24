using Unity.Entities;
using UnityEngine;
using Unity.Scenes;
using Unity.Collections;
using RPG.Core;
using Unity.Jobs;

namespace RPG.Saving
{

    [UpdateInGroup(typeof(SavingSystemGroup))]

    public class StatefulSceneSystem : SystemBase
    {
        SaveSystem saveSystem;
        SceneSystem sceneSystem;
        EntityQuery sceneNeedSavingQuery;

        EntityQuery sceneNeedLoadingQuery;

        EntityQuery sceneEntityQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            saveSystem = World.GetOrCreateSystem<SaveSystem>();
            sceneSystem = World.GetOrCreateSystem<SceneSystem>();
            sceneEntityQuery = GetEntityQuery(typeof(SceneTag), typeof(Identifier));
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc()
            {
                Any = new ComponentType[] {
                    typeof(TriggerUnloadScene),
                     typeof(SceneLoaded)
                }
            }));
        }

        protected override void OnUpdate()
        {
            SaveDataOnUnload();
            LoadDataOnLoad();

        }

        private void SaveDataOnUnload()
        {
            var loadScenesAsync = new NativeArray<TriggerUnloadScene>(sceneNeedSavingQuery.CalculateEntityCount(), Allocator.TempJob);
            var sceneNeedSavingHandle = Entities
             .WithStoreEntityQueryInField(ref sceneNeedSavingQuery)
             .ForEach((int entityInQueryIndex, in TriggerUnloadScene loadSceneAsync) =>
             {
                 loadScenesAsync[entityInQueryIndex] = loadSceneAsync;

             }).ScheduleParallel(default);
            sceneNeedSavingHandle.Complete();
            for (int i = 0; i < loadScenesAsync.Length; i++)
            {
                var sceneEntity = sceneSystem.GetSceneEntity(loadScenesAsync[i].SceneGUID);
                var resolvedSections = EntityManager.GetBuffer<ResolvedSectionEntity>(sceneEntity);
                foreach (var resolvedSection in resolvedSections)
                {
                    sceneEntityQuery.AddSharedComponentFilter(new SceneTag() { SceneEntity = resolvedSection.SectionEntity });
                    var count = sceneEntityQuery.CalculateEntityCount();
                    if (count > 0)
                    {
                        Debug.Log($"Saving Scene State for: {resolvedSection.SectionEntity} {count}");
                        saveSystem.Save(sceneEntityQuery);
                    }
                    sceneEntityQuery.ResetFilter();
                }

            }
            loadScenesAsync.Dispose();
            Dependency = JobHandle.CombineDependencies(Dependency, sceneNeedSavingHandle);
        }
        private void LoadDataOnLoad()
        {
            var sceneNeedLoadingCount = sceneNeedLoadingQuery.CalculateEntityCount();
            var loadScenesAsync = new NativeArray<SceneLoaded>(sceneNeedLoadingCount, Allocator.TempJob);
            var sceneNeedLoadingHandle = Entities
            .WithStoreEntityQueryInField(ref sceneNeedLoadingQuery)
            .ForEach((int entityInQueryIndex, in SceneLoaded sceneLoaded) =>
            {
                loadScenesAsync[entityInQueryIndex] = sceneLoaded;
            }).ScheduleParallel(default);
            sceneNeedLoadingHandle.Complete();

            for (int i = 0; i < loadScenesAsync.Length; i++)
            {
                var sceneEntity = sceneSystem.GetSceneEntity(loadScenesAsync[i].SceneGUID);
                var resolvedSections = EntityManager.GetBuffer<ResolvedSectionEntity>(sceneEntity);
                foreach (var resolvedSection in resolvedSections)
                {
                    sceneEntityQuery.AddSharedComponentFilter(new SceneTag() { SceneEntity = resolvedSection.SectionEntity });
                    var count = sceneEntityQuery.CalculateEntityCount();
                    if (count > 0)
                    {
                        Debug.Log($"Loading Scene State for: {resolvedSection.SectionEntity} {count}");
                        saveSystem.Load(sceneEntityQuery);
                    }
                    sceneEntityQuery.ResetFilter();
                }

            }
            loadScenesAsync.Dispose();
            Dependency = JobHandle.CombineDependencies(Dependency, sceneNeedLoadingHandle);
        }
    }
}

