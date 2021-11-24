using Unity.Entities;
using UnityEngine;
using Unity.Scenes;
using Unity.Collections;
using RPG.Core;
using Unity.Jobs;

namespace RPG.Saving
{

    [UpdateInGroup(typeof(CoreSystemGroup))]
    [UpdateBefore(typeof(SceneLoadingSystem))]
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
            sceneEntityQuery = GetEntityQuery(typeof(Identifier), typeof(SceneTag));
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
                    Debug.Log($"Saving Scene State for: {resolvedSection.SectionEntity} " + sceneEntityQuery.CalculateEntityCount());
                }
                saveSystem.Save(sceneEntityQuery);
                sceneEntityQuery.ResetFilter();
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

            Debug.Log($"LoadDataOnLoad  {sceneNeedLoadingCount} scenes need loading");
            for (int i = 0; i < loadScenesAsync.Length; i++)
            {
                var sceneEntity = sceneSystem.GetSceneEntity(loadScenesAsync[i].SceneGUID);
                Debug.Log($"Loading Scene State for: {loadScenesAsync[i].SceneGUID} ");
                var resolvedSections = EntityManager.GetBuffer<ResolvedSectionEntity>(sceneEntity);
                foreach (var resolvedSection in resolvedSections)
                {
                    sceneEntityQuery.AddSharedComponentFilter(new SceneTag() { SceneEntity = resolvedSection.SectionEntity });
                    Debug.Log($"Loading Scene State for: {resolvedSection.SectionEntity} " + sceneEntityQuery.CalculateEntityCount());
                }
                saveSystem.Load(sceneEntityQuery);
                sceneEntityQuery.ResetFilter();
            }
            loadScenesAsync.Dispose();
            Dependency = JobHandle.CombineDependencies(Dependency, sceneNeedLoadingHandle);
        }
    }
}

