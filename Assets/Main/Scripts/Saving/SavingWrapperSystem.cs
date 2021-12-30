using System.IO;
using RPG.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RPG.Saving
{
    public struct NewGame : IComponentData { }
    public struct GameLoaded : IComponentData { }
    public struct TriggerNewGame : IComponentData
    {

    }
    public struct TriggerSave : IComponentData { }

    public struct TriggerLoad : IComponentData
    {

    }
    public class TriggerSavingSystem : SystemBase
    {
        SavingWrapperSystem savingDebugSystem;
        EntityQuery triggerNewGameQuery;
        EntityQuery triggerLoadQuery;
        EntityQuery triggerSaveQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            savingDebugSystem = World.GetOrCreateSystem<SavingWrapperSystem>();
        }
        protected override void OnUpdate()
        {
            Entities
            .WithStoreEntityQueryInField(ref triggerNewGameQuery)
            .WithAny<TriggerNewGame>()
            .ForEach((Entity e) =>
            {
                savingDebugSystem.NewGame(e);
            })
            .WithStructuralChanges()
            .Run();
            Entities
            .WithAll<TriggerLoad>()
            .WithNone<LoadingScene>()
            .WithStoreEntityQueryInField(ref triggerLoadQuery)
            .ForEach((Entity e) =>
            {
                savingDebugSystem.LoadDefaultSave(e);
            })
            .WithStructuralChanges()
            .Run();

            Entities
            .WithAll<TriggerSave>()
            .WithStoreEntityQueryInField(ref triggerSaveQuery)
            .ForEach((Entity e) =>
            {
                savingDebugSystem.Save();
            })
            .WithStructuralChanges()
            .Run();
            EntityManager.RemoveComponent<TriggerNewGame>(triggerNewGameQuery);
            EntityManager.RemoveComponent<TriggerLoad>(triggerLoadQuery);
            EntityManager.RemoveComponent<TriggerSave>(triggerSaveQuery);
        }

    }

    [UpdateInGroup(typeof(SavingSystemGroup))]
    public class SavingWrapperSystem : SystemBase
    {

        BeginPresentationEntityCommandBufferSystem entityCommandBufferSystem;
        SaveSystemBase saveSystem;
        EntityQuery requestForUpdateQuery;
        EntityQuery gameSettingQuery;
        EntityQuery newGameQuery;

        string savePath;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
            saveSystem = World.GetOrCreateSystem<SaveSystemBase>();
            requestForUpdateQuery = GetEntityQuery(new EntityQueryDesc()
            {
                None = new ComponentType[] {
                    typeof(TriggerSceneLoad),
                    typeof(TriggeredSceneLoaded),
                    typeof(LoadSceneAsync),
                    typeof(UnloadScene),
                    typeof(AnySceneLoading)
                }
            });

            gameSettingQuery = GetEntityQuery(ComponentType.ReadOnly<GameSettings>());
            newGameQuery = GetEntityQuery(ComponentType.ReadOnly<NewGame>());
            savePath = SaveSystem.GetPathFromSaveFile("test.save");
            RequireForUpdate(requestForUpdateQuery);

        }

        public bool HasSave()
        {
            return File.Exists(savePath);
        }
        public void Save()
        {
            Debug.Log("Saving in file");

            saveSystem.Save(savePath);
        }
        public void LoadDefaultSave(Entity triggerEntity)
        {
            SceneLoadingSystem.UnloadAllCurrentlyLoadedScene(EntityManager);
            var gameSettings = gameSettingQuery.GetSingleton<GameSettings>();
            var gameSettingsEntity = gameSettingQuery.GetSingletonEntity();
            TriggerSceneLoad(gameSettingsEntity, gameSettings.PlayerScene);
            saveSystem.LoadLastScene(triggerEntity, savePath);
        }
        public void NewGame(Entity triggerEntity)
        {

            if (HasSave())
            {
                File.Delete(savePath);
            }
            SceneLoadingSystem.UnloadAllCurrentlyLoadedScene(EntityManager);
            var gameSettings = gameSettingQuery.GetSingleton<GameSettings>();
            var gameSettingsEntity = gameSettingQuery.GetSingletonEntity();
            TriggerSceneLoad(triggerEntity, gameSettings.NewGameScene);
            TriggerSceneLoad(gameSettingsEntity, gameSettings.PlayerScene);
            EntityManager.AddComponent<NewGame>(triggerEntity);
        }

        private void TriggerSceneLoad(Entity sceneLoadEventEntity, Unity.Entities.Hash128 sceneGUID)
        {

            EntityManager.AddComponent<DontLoadSave>(sceneLoadEventEntity);
            EntityManager.AddComponentData(sceneLoadEventEntity, new TriggerSceneLoad() { SceneGUID = sceneGUID });
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            cb.RemoveComponentForEntityQuery<NewGame>(newGameQuery);
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.altKey.isPressed && keyboard.nKey.wasPressedThisFrame)
                {
                    var gameSettingsEntity = gameSettingQuery.GetSingletonEntity();
                    NewGame(gameSettingsEntity);
                }
                if (keyboard.altKey.isPressed && keyboard.sKey.wasPressedThisFrame)
                {
                    //FIXME: Save should not be called directly the save system should react to a component that request a save
                    // saveSystem.Save();
                    Save();
                }
                if (keyboard.altKey.isPressed && keyboard.lKey.wasPressedThisFrame)
                {
                    //FIXME: Save should not be called directly the save system should react to a component that request a load
                    var triggerEntity = EntityManager.CreateEntity();
                    LoadDefaultSave(triggerEntity);
                }
            }

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }
}

