using RPG.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RPG.Saving
{
    [UpdateInGroup(typeof(SavingSystemGroup))]
    [UpdateAfter(typeof(SaveSystemBase))]
    public class SavingDebugSystem : SystemBase
    {

        SaveSystemBase saveSystem;
        EntityQuery requestForUpdateQuery;

        string savePath;
        protected override void OnCreate()
        {
            base.OnCreate();
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
            savePath = SaveSystem.GetPathFromSaveFile("test.save");

            RequireForUpdate(requestForUpdateQuery);

        }
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            if (!saveSystem.LoadLastScene(savePath))
            {
                NewGame();
            }
        }

        protected void NewGame()
        {

            SceneLoadingSystem.UnloadAllCurrentlyLoadedScene(EntityManager);
            var gameSettingsEntity = GetSingletonEntity<GameSettings>();
            var gameSettings = GetSingleton<GameSettings>();
            EntityManager.AddComponentData(gameSettingsEntity, new TriggerSceneLoad() { SceneGUID = gameSettings.NewGameScene });
        }
        protected override void OnUpdate()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.altKey.isPressed && keyboard.nKey.wasPressedThisFrame)
                {
                    // NewGame();
                }
                if (keyboard.altKey.isPressed && keyboard.sKey.wasPressedThisFrame)
                {
                    Debug.Log("Saving in file");
                    //FIXME: Save should not be called directly the save system should react to a component that request a save
                    // saveSystem.Save();
                    saveSystem.Save(savePath);
                }
                if (keyboard.altKey.isPressed && keyboard.lKey.wasPressedThisFrame)
                {
                    SceneLoadingSystem.UnloadAllCurrentlyLoadedScene(EntityManager);
                    //FIXME: Load should not be called directly the save system should react to a component that request a Load
                    // saveSystem.Load();
                    saveSystem.LoadLastScene(savePath);
                }
            }
        }

    }
}

