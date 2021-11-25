using Unity.Entities;
using UnityEngine;
using Unity.Scenes;
using Unity.Collections;
using RPG.Core;
using Unity.Jobs;

namespace RPG.Saving
{

    [UpdateInGroup(typeof(SavingSystemGroup))]
    [UpdateAfter(typeof(SpawnableIdentifiableSystem))]

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
            sceneNeedSavingQuery = GetEntityQuery(typeof(TriggerUnloadScene));
            sceneNeedLoadingQuery = GetEntityQuery(typeof(SceneLoaded));
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

            var loadScenesAsync = sceneNeedSavingQuery.ToComponentDataArrayAsync<TriggerUnloadScene>(Allocator.TempJob, out var sceneNeedSavingHandle);
            sceneNeedSavingHandle.Complete();
            for (int i = 0; i < loadScenesAsync.Length; i++)
            {
                SaveScene(loadScenesAsync[i].SceneGUID);
            }
            loadScenesAsync.Dispose();
            Dependency = JobHandle.CombineDependencies(Dependency, sceneNeedSavingHandle);
        }

        public void SaveScene(Unity.Entities.Hash128 sceneGUID)
        {
            var sceneEntity = sceneSystem.GetSceneEntity(sceneGUID);
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
        public void LoadScene(Unity.Entities.Hash128 sceneGUID)
        {
            var sceneEntity = sceneSystem.GetSceneEntity(sceneGUID);
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
        private void LoadDataOnLoad()
        {
            var sceneNeedLoadingCount = sceneNeedLoadingQuery.CalculateEntityCount();
            var loadScenesAsync = sceneNeedLoadingQuery.ToComponentDataArray<SceneLoaded>(Allocator.Temp);
            for (int i = 0; i < loadScenesAsync.Length; i++)
            {
                LoadScene(loadScenesAsync[i].SceneGUID);
            }
            loadScenesAsync.Dispose();
        }
    }
}

