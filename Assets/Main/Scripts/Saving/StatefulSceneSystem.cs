using Unity.Entities;
using UnityEngine;
using Unity.Scenes;
using Unity.Collections;
using RPG.Core;

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
        protected override void OnCreate()
        {
            base.OnCreate();
            saveSystem = World.GetOrCreateSystem<SaveSystem>();
            sceneSystem = World.GetOrCreateSystem<SceneSystem>();
            RequireForUpdate(sceneNeedSavingQuery);
            RequireForUpdate(sceneNeedLoadingQuery);
        }

        protected override void OnUpdate()
        {
            SaveDataOnUnload();
            LoadDataOnLoad();
        }

        private void SaveDataOnUnload()
        {
            var loadScenesAsync = new NativeArray<TriggerUnloadScene>(sceneNeedSavingQuery.CalculateEntityCount(), Allocator.TempJob);
            Entities
            .WithStoreEntityQueryInField(ref sceneNeedSavingQuery)
            .ForEach((int entityInQueryIndex, in TriggerUnloadScene loadSceneAsync) =>
            {
                loadScenesAsync[entityInQueryIndex] = loadSceneAsync;

            }).ScheduleParallel();
            Dependency.Complete();
            var query = EntityManager.CreateEntityQuery(typeof(Identifier), typeof(SceneTag));
            for (int i = 0; i < loadScenesAsync.Length; i++)
            {
                var sceneEntity = sceneSystem.GetSceneEntity(loadScenesAsync[i].SceneGUID);
                var resolvedSections = EntityManager.GetBuffer<ResolvedSectionEntity>(sceneEntity);
                foreach (var resolvedSection in resolvedSections)
                {
                    query.AddSharedComponentFilter(new SceneTag() { SceneEntity = resolvedSection.SectionEntity });
                    Debug.Log($"Saving Scene State for: {resolvedSection.SectionEntity} " + query.CalculateEntityCount());
                }
                saveSystem.Save(query);
                query.ResetFilter();
            }
            loadScenesAsync.Dispose();
        }
        private void LoadDataOnLoad()
        {
            var loadScenesAsync = new NativeArray<LoadSceneAsync>(sceneNeedLoadingQuery.CalculateEntityCount(), Allocator.TempJob);
            Entities
            .WithStoreEntityQueryInField(ref sceneNeedLoadingQuery)
            .ForEach((int entityInQueryIndex, in LoadSceneAsync loadSceneAsync) =>
            {
                loadScenesAsync[entityInQueryIndex] = loadSceneAsync;

            }).ScheduleParallel();
            Dependency.Complete();
            var query = EntityManager.CreateEntityQuery(typeof(Identifier), typeof(SceneTag));
            for (int i = 0; i < loadScenesAsync.Length; i++)
            {
                var sceneEntity = sceneSystem.GetSceneEntity(loadScenesAsync[i].SceneGUID);
                var resolvedSections = EntityManager.GetBuffer<ResolvedSectionEntity>(sceneEntity);
                Debug.Log($"Loading Scene State for: {sceneEntity} " + query.CalculateEntityCount());
                foreach (var resolvedSection in resolvedSections)
                {
                    query.AddSharedComponentFilter(new SceneTag() { SceneEntity = resolvedSection.SectionEntity });
                    Debug.Log($"Loading Scene State for: {resolvedSection.SectionEntity} " + query.CalculateEntityCount());
                }
                saveSystem.Load(query);
                query.ResetFilter();
            }
            loadScenesAsync.Dispose();
        }
    }
}

