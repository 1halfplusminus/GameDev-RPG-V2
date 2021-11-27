using Unity.Entities;
using UnityEngine;
using Unity.Scenes;
using Unity.Collections;
using RPG.Core;
using Unity.Jobs;

namespace RPG.Saving
{
    public struct DontLoadSceneState : IComponentData { }

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
            sceneEntityQuery = GetEntityQuery(typeof(SceneSection), typeof(Identifier));
            sceneNeedSavingQuery = GetEntityQuery(typeof(TriggerUnloadScene));
            sceneNeedLoadingQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(TriggeredSceneLoaded)
                },
                None = new ComponentType[] {
                    typeof(DontLoadSceneState)
                }
            });
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc()
            {
                Any = new ComponentType[] {
                    typeof(TriggerUnloadScene),
                    typeof(TriggeredSceneLoaded)
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
            sceneEntityQuery.AddSharedComponentFilter(new SceneSection() { SceneGUID = sceneGUID });
            var count = sceneEntityQuery.CalculateEntityCount();
            if (count > 0)
            {
                Debug.Log($"Saving Scene State for: {sceneGUID} {count}");
                saveSystem.Save(sceneEntityQuery, SavingStateType.SCENE);
            }
            sceneEntityQuery.ResetFilter();
        }
        public void LoadScene(Unity.Entities.Hash128 sceneGUID)
        {
            var sceneEntity = sceneSystem.GetSceneEntity(sceneGUID);
            sceneEntityQuery.AddSharedComponentFilter(new SceneSection() { SceneGUID = sceneGUID });
            var count = sceneEntityQuery.CalculateEntityCount();
            if (count > 0)
            {
                Debug.Log($"Loading Scene State for: {sceneGUID} {count}");
                saveSystem.LoadSerializedWorld(SavingStateType.SCENE);
            }
            sceneEntityQuery.ResetFilter();
        }
        private void LoadDataOnLoad()
        {
            var sceneNeedLoadingCount = sceneNeedLoadingQuery.CalculateEntityCount();
            var loadScenesAsync = sceneNeedLoadingQuery.ToComponentDataArray<TriggeredSceneLoaded>(Allocator.Temp);
            for (int i = 0; i < loadScenesAsync.Length; i++)
            {
                LoadScene(loadScenesAsync[i].SceneGUID);
            }
            loadScenesAsync.Dispose();
        }
    }
}

