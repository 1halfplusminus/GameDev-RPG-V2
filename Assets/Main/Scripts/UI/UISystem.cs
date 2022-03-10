using System;
using RPG.Combat;
using RPG.Control;
using RPG.Core;
using RPG.Saving;
using RPG.Stats;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;
using static Unity.Entities.ComponentType;
using static RPG.UI.UIExtensions;

namespace RPG.UI
{
    public struct InGame : IComponentData
    {
    }
    public struct InPause : IComponentData
    {
    }
    public struct Initialized : IComponentData
    {
    }
    public struct Destroy : IComponentData
    {
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class GameOverUISystem : SystemBase
    {
        public string GAME_OVER_UI_ADDRESS = "GameOverUI";
        private AsyncOperationHandle<GameObject> handle;
        private Entity gameOverPrefab;
        private EntityCommandBufferSystem entityCommandBufferSystem;
        private SavingWrapperSystem savingSystem;
        private MainGameUISystem mainGameUISystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            LoadGameUIAddressable();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            savingSystem = World.GetOrCreateSystem<SavingWrapperSystem>();
            mainGameUISystem = World.GetOrCreateSystem<MainGameUISystem>();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Addressables.Release(handle);
        }
        private void HandleCompleted(AsyncOperationHandle<GameObject> operation)
        {
            if (operation.Status == AsyncOperationStatus.Succeeded)
            {
                var convertToEntitySystem = World.GetExistingSystem<ConvertToEntitySystem>();
                var conversionSetting = GameObjectConversionSettings.FromWorld(World, convertToEntitySystem.BlobAssetStore);
                gameOverPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(operation.Result, conversionSetting);
            }
            else
            {
                Debug.LogError($"Asset for gameover ui failed to load.");
            }
        }

        private void LoadGameUIAddressable()
        {
            if (!handle.IsValid())
            {
                handle = Addressables.LoadAssetAsync<GameObject>(GAME_OVER_UI_ADDRESS);
                handle.Completed += HandleCompleted;
            }
        }

        private Action OnMainMenuReload(Entity e)
        {
            return () => mainGameUISystem.ReloadMainMenu(e);
        }
        private Action OnTryAgain(Entity e)
        {
            return () => savingSystem.LoadDefaultSave(e);
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var unLoadScene = false;
            Entities
            .WithNone<GameOverUI>()
            .WithAll<PlayerControlled, IsDeadTag>()
            .ForEach((Entity e) =>
            {
                if (gameOverPrefab != Entity.Null)
                {
                    var instance = cb.Instantiate(gameOverPrefab);
                    cb.AddComponent<GameOverUI>(e);
                    unLoadScene = true;
                }
            }).WithoutBurst().Run();
            if (unLoadScene)
            {
                SceneLoadingSystem.UnloadAllCurrentlyLoadedScene(EntityManager);
            }
            Entities
            .WithAll<GameOverUI, UIReady>()
            .WithNone<GameOverUIController>()
            .ForEach((Entity e, UIDocument document) =>
            {
                var controller = new GameOverUIController();
                controller.OnTryAgain += OnTryAgain(e);
                controller.OnMainMenu += OnMainMenuReload(e);
                controller.Init(document.rootVisualElement);
                cb.AddComponent(e, controller);
            })
            .WithoutBurst()
            .Run();

            Entities
            .WithAll<GameOverUI, TriggeredSceneLoaded, UIReady>()
            .ForEach((Entity e) => cb.DestroyEntity(e)).Schedule();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

    public struct InGameUIFor : IComponentData
    {
        public Entity Entity;
    }
    public struct InGameUIInstance : IComponentData
    {
        public Entity Entity;
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class InGameUISystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery displayInGameUIQuery;

        EntityQuery playerQuery;

        EntityQuery prefabQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            prefabQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    ReadOnly<InGameUI>(),
                    ReadOnly<Prefab>()
                }
            });

            displayInGameUIQuery = GetEntityQuery(new EntityQueryDesc()
            {
                Any = new ComponentType[] {
                    ReadOnly<InGameUI>()
                }
            });
            RequireForUpdate(prefabQuery);
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var prefab = prefabQuery.GetSingletonEntity();
            Entities
            .WithNone<InGameUIInstance>()
            .WithAll<PlayerControlled>()
            .ForEach((Entity e) =>
            {
                var instance = cb.Instantiate(prefab);
                cb.AddComponent(instance, new InGameUIFor { Entity = e });
                cb.AddComponent(e, new InGameUIInstance { Entity = instance });
            })
            .WithoutBurst()
            .Run();
            Entities
            .ForEach((in InGameUIController c, in Health playerHealth, in BaseStats baseStats, in ExperiencePoint experience) =>
            {
                c.SetPlayerHealth(playerHealth, baseStats.Level, baseStats.ProgressionAsset);
                c.SetExperiencePoint(experience.Value);
                c.SetLevel(baseStats);
            })
            .WithoutBurst()
            .Run();

            Entities
            .WithAll<UIReady, InGameUI>()
            .WithNone<InGameUIController>()
            .ForEach((Entity e, in InGameUIFor ingameUIFor, in UIDocument uiDocument) =>
            {
                var controller = new InGameUIController();
                controller.Init(uiDocument.rootVisualElement);
                cb.AddComponent(e, controller);
                cb.AddComponent(ingameUIFor.Entity, controller);
            })
            .WithoutBurst()
            .Run();

            Entities
            // .WithReadOnly(em)
           .ForEach((Entity e, in InGameUIFor uiFor) =>
           {
               var componentDataFromEntity = GetComponentDataFromEntity<InGameUIInstance>(true);
               if (!componentDataFromEntity.HasComponent(uiFor.Entity))
               {
                   cb.DestroyEntity(e);
               }
           })
           .Schedule();
        }
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class InPauseUISystem : SystemBase
    {
        InputSystem inputSystem;
        SavingWrapperSystem saveSystem;

        PauseSystem pauseSystem;
        EntityQuery instantiatingPauseUIQuery;
        EntityQuery pauseUIQuery;

        MainGameUISystem mainGameUISystem;

        EntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            mainGameUISystem = World.GetOrCreateSystem<MainGameUISystem>();
            inputSystem = World.GetOrCreateSystem<InputSystem>();
            saveSystem = World.GetOrCreateSystem<SavingWrapperSystem>();
            pauseSystem = World.GetOrCreateSystem<PauseSystem>();
            instantiatingPauseUIQuery = GetEntityQuery(ReadOnly<PauseUI>(), ReadOnly<Prefab>());
            pauseUIQuery = GetEntityQuery(ReadOnly<PauseUI>());
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc()
            {
                Any = new ComponentType[] {
                    ReadOnly<SceneLoaded>()
                }
            }));
        }
        private void InitGUI(Entity e, VisualElement ui)
        {
            var mainMenu = ui.Q<Button>("MainMenu");
            ui.Q<Button>("Save").clicked += Save;
            ui.Q<Button>("Exit").clicked += QuitGame;
            ui.Q<Button>("MainMenu").clicked += OnMainMenuReload(e, mainMenu);
        }
        private Action OnMainMenuReload(Entity e, Button button)
        {
            return () =>
            {
                button.SetEnabled(false);
                mainGameUISystem.ReloadMainMenu(e);
                SetPause(false);
                button.SetEnabled(true);
            };
        }
        private void Save()
        {
            Debug.Log("Saving game");
            saveSystem.Save();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var input = inputSystem.Input;
            var inGameHUDControllerPausePressed = false;
            Entities.ForEach((InGameUIController inGameHUDController) =>
            {
                if (inGameHUDController.SettingClicked)
                {
                    inGameHUDController.SettingClicked = false;
                    inGameHUDControllerPausePressed = true;
                }
            }).WithoutBurst().Run();
            var pausePressed = inGameHUDControllerPausePressed || input.UI.TogglePauseMenu.WasPressedThisFrame();
            if (pausePressed)
            {
                if (pauseUIQuery.CalculateEntityCount() == 0)
                {
                    var toInstanciate = instantiatingPauseUIQuery.GetSingletonEntity();
                    var uiEntity = EntityManager.Instantiate(toInstanciate);
                    SetPause(true);
                    var uiDocument = EntityManager.GetComponentObject<UIDocument>(uiEntity);
                }
                else
                {
                    EntityManager.DestroyEntity(pauseUIQuery.GetSingletonEntity());
                    SetPause(false);
                }
                Debug.Log($"Toggle Pause Menu Pressed");
            }
            Entities
            .WithAll<PauseUI, UIReady>()
            .WithNone<Initialized>()
            .ForEach((Entity e, UIDocument document) =>
            {
                InitGUI(e, document.rootVisualElement);
                cb.AddComponent<Initialized>(e);
            })
            .WithoutBurst()
            .Run();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        private void SetPause(bool paused)
        {
            pauseSystem.Pause(paused);
        }
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class MainGameUISystem : SystemBase
    {
        SavingWrapperSystem savingWrapperSystem;

        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery uiDocumentQuery;

        EntityQuery gameEventListener;

        EntityQuery mainMenuUIPrefabsQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            mainMenuUIPrefabsQuery = GetEntityQuery(new ComponentType[] { ReadOnly<NewGameUI>(), ReadOnly<Prefab>() });
            savingWrapperSystem = World.GetOrCreateSystem<SavingWrapperSystem>();
            uiDocumentQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                None = new ComponentType[] {
                    ReadOnly<Initialized>()
                },
                All = new ComponentType[]{
                    ReadOnly<UIReady>(), ReadOnly<NewGameUI>(),typeof(UIDocument)
                }
            });
            gameEventListener = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] {
                  ReadOnly<GameEventListener>(),ReadOnly<Prefab>()
                },
            });
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]{
                    ReadOnly<UIReady>(), ReadOnly<NewGameUI>(),typeof(UIDocument)
                }
            }));
        }
        public void ReloadMainMenu(Entity trigger)
        {
            SceneLoadingSystem.UnloadAllCurrentlyLoadedScene(EntityManager);
            var mainMenuEntity = GetMainMenuPrefab();
            EntityManager.Instantiate(mainMenuEntity);
            if (trigger != Entity.Null)
            {
                EntityManager.DestroyEntity(trigger);
            }
        }
        public Entity GetMainMenuPrefab()
        {
            return mainMenuUIPrefabsQuery.GetSingletonEntity();
        }
        protected void NewGame(Entity e)
        {
            EntityManager.AddComponent<TriggerNewGame>(e);
            EntityManager.AddComponent<InGame>(gameEventListener);
            Debug.Log($"Click on new game listener {gameEventListener.CalculateEntityCount()}");
        }
        protected void LoadSave(Entity e)
        {
            EntityManager.AddComponent<TriggerLoad>(e);
            EntityManager.AddComponent<InGame>(gameEventListener);
            Debug.Log($"Click on load save");
        }
        protected override void OnUpdate()
        {
            var em = EntityManager;
            var ec = entityCommandBufferSystem.CreateCommandBuffer();
            var ecp = ec.AsParallelWriter();
            Entities
            .WithAll<UIReady>()
            .WithAny<TriggeredSceneLoaded>()
            .ForEach((Entity e, UIDocument _) => ec.DestroyEntity(e))
            .WithoutBurst()
            .Run();

            if (uiDocumentQuery.CalculateEntityCount() > 0)
            {
                InitNewGameUI();
            }
            em.RemoveComponent<InGame>(gameEventListener);
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        private void InitNewGameUI()
        {
            var uiDocumentEntity = uiDocumentQuery.GetSingletonEntity();
            var uiDocument = EntityManager.GetComponentObject<UIDocument>(uiDocumentEntity);
            var visualElement = uiDocument.rootVisualElement;
            InitExitButton(uiDocumentEntity, visualElement);
            InitNewGameButton(uiDocumentEntity, visualElement);
            InitLoadButton(uiDocumentEntity, visualElement);
            EntityManager.AddComponent<Initialized>(uiDocumentEntity);
        }
        private void InitExitButton(Entity uiDocumentEntity, VisualElement visualElement)
        {
            var button = visualElement
            .Q<Button>("Exit");
            button
            .clicked += () => QuitGame();
        }

        private void InitLoadButton(Entity uiDocumentEntity, VisualElement visualElement)
        {
            var loadButton = visualElement
            .Q<Button>("Load");
            loadButton
            .clicked += () => LoadSave(uiDocumentEntity);
            if (!savingWrapperSystem.HasSave())
            {
                loadButton.SetEnabled(false);
            }
        }

        private void InitNewGameButton(Entity uiDocumentEntity, VisualElement visualElement)
        {
            var newGameButton = visualElement.Q<Button>("NewGame");
            newGameButton.clicked += () =>
            {
                NewGame(uiDocumentEntity);
                visualElement.style.visibility = Visibility.Hidden;
            };
        }
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class AutoInstantiateGameUISystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            Entities.WithAll<GameUI, Prefab, AutoInstantiateUI>().ForEach((Entity e) =>
            {
                Debug.Log($"Instanciate {e.Index}");
                cb.Instantiate(e);
                cb.RemoveComponent<AutoInstantiateUI>(e);
            })
            .Run();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}