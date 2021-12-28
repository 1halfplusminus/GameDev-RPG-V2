using RPG.Combat;
using RPG.Control;
using RPG.Core;
using RPG.Saving;
using RPG.Stats;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Entities.ComponentType;
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
    [UpdateInGroup(typeof(UISystemGroup))]
    public class InGameUISystem : SystemBase
    {
        EntityQuery displayInGameUIQuery;

        EntityQuery playerQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            playerQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]{
                    ReadOnly<PlayerControlled>(),
                    ReadOnly<Health>(),
                    ReadOnly<Fighter>(),
                    ReadOnly<ExperiencePoint>(),
                    ReadOnly<BaseStats>()
                }
            });
            playerQuery.SetChangedVersionFilter(ReadOnly<Health>());
            playerQuery.SetChangedVersionFilter(ReadOnly<Fighter>());
            // playerQuery.SetChangedVersionFilter(ReadOnly<ExperiencePoint>());
            displayInGameUIQuery = GetEntityQuery(new EntityQueryDesc()
            {
                Any = new ComponentType[] {
                    ReadOnly<InGameUI>()
                }
            });

        }
        protected override void OnUpdate()
        {
            if (playerQuery.CalculateEntityCount() == 1)
            {
                var playerHealth = playerQuery.GetSingleton<Health>();
                var fighter = playerQuery.GetSingleton<Fighter>();
                var experience = playerQuery.GetSingleton<ExperiencePoint>();
                var baseStats = playerQuery.GetSingleton<BaseStats>();
                Entities
                .ForEach((InGameUIController c) =>
                {
                    c.SetPlayerHealth(playerHealth, experience.GetLevel(baseStats.ProgressionAsset), baseStats.ProgressionAsset);
                    c.SetEnemyHealth(fighter.Target, EntityManager);
                    c.SetExperiencePoint(experience.Value);
                    c.SetLevel(experience, baseStats.ProgressionAsset);
                })
                .WithoutBurst().Run();
            }

            Entities
            .WithAll<UIReady, InGameUI>()
            .WithNone<InGameUIController>()
            .ForEach((Entity e, UIDocument uiDocument) =>
            {
                var controller = new InGameUIController();
                controller.Init(uiDocument.rootVisualElement);
                EntityManager.AddComponentObject(e, controller);
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            if (displayInGameUIQuery.CalculateEntityCount() == 0)
            {
                Entities
                .WithAll<InGameUI, Prefab, InGame>()
                .WithNone<InGameUIController>()
                .ForEach((Entity e) =>
                {
                    EntityManager.Instantiate(e);
                })
                .WithStructuralChanges()
                .WithoutBurst()
                .Run();
            }

        }


    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class InPauseUISystem : SystemBase
    {
        InputSystem inputSystem;
        SavingDebugSystem saveSystem;

        PauseSystem pauseSystem;
        EntityQuery instantiatingPauseUIQuery;
        EntityQuery pauseUIQuery;


        protected override void OnCreate()
        {
            base.OnCreate();
            inputSystem = World.GetOrCreateSystem<InputSystem>();
            saveSystem = World.GetOrCreateSystem<SavingDebugSystem>();
            pauseSystem = World.GetOrCreateSystem<PauseSystem>();
            instantiatingPauseUIQuery = GetEntityQuery(ReadOnly<PauseUI>(), ReadOnly<Prefab>());
            pauseUIQuery = GetEntityQuery(ReadOnly<PauseUI>());
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc()
            {
                Any = new ComponentType[] {
                    ReadOnly<SceneLoaded>()
                }
            }));
        }
        private void InitGUI(VisualElement ui)
        {
            ui.Q<Button>("Save").clicked += Save;
            ui.Q<Button>("Exit").clicked += QuitGame;
        }
        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
        private void Save()
        {
            Debug.Log("Saving game");
            saveSystem.Save();
        }
        protected override void OnUpdate()
        {
            var input = inputSystem.Input;
            if (input.UI.TogglePauseMenu.WasPressedThisFrame())
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
            Entities.WithAll<PauseUI, UIReady>().ForEach((UIDocument document) => InitGUI(document.rootVisualElement)).WithoutBurst().Run();
        }

        private void SetPause(bool paused)
        {
            pauseSystem.Pause(paused);
        }
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class MainGameUISystem : SystemBase
    {

        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery uiDocumentQuery;

        EntityQuery gameEventListener;

        protected override void OnCreate()
        {
            base.OnCreate();
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
                  ReadOnly<GameEventListener>(),     ReadOnly<Prefab>()
                },
                // Any = new ComponentType[] {
                //   ReadOnly<Prefab>()
                // },
            });
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]{
                    ReadOnly<UIReady>(), ReadOnly<NewGameUI>(),typeof(UIDocument)
                }
            }));
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
            .ForEach((int entityInQueryIndex, Entity e, UIDocument uiDocument) =>
            {
                ec.DestroyEntity(e);
            }).WithoutBurst().Run();

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
            InitNewGameButton(uiDocumentEntity, visualElement);
            InitLoadButton(uiDocumentEntity, visualElement);
            EntityManager.AddComponent<Initialized>(uiDocumentEntity);
        }

        private void InitLoadButton(Entity uiDocumentEntity, VisualElement visualElement)
        {
            visualElement
            .Q<Button>("Load")
            .clicked += () => { LoadSave(uiDocumentEntity); };
        }

        private void InitNewGameButton(Entity uiDocumentEntity, VisualElement visualElement)
        {
            var newGameButton = visualElement.Q<Button>("NewGame");
            newGameButton.clicked += () => { NewGame(uiDocumentEntity); };
        }
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class AutoInstantiateGameUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var em = EntityManager;
            Entities.WithAll<GameUI, Prefab, AutoInstantiateUI>().ForEach((Entity e) =>
            {
                Debug.Log($"Instanciate {e.Index}");
                em.Instantiate(e);
                em.RemoveComponent<AutoInstantiateUI>(e);
            }).WithStructuralChanges().WithoutBurst().Run();
        }
    }

}