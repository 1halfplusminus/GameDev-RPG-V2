using RPG.Combat;
using RPG.Control;
using RPG.Core;
using RPG.Mouvement;
using RPG.Saving;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.UI
{
    public struct Initialized : IComponentData
    {

    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class InPauseUISystem : SystemBase
    {
        InputSystem inputSystem;
        SavingDebugSystem saveSystem;
        EntityQuery instantiatingPauseUIQuery;
        EntityQuery pauseUIQuery;


        protected override void OnCreate()
        {
            base.OnCreate();
            inputSystem = World.GetOrCreateSystem<InputSystem>();
            saveSystem = World.GetOrCreateSystem<SavingDebugSystem>();
            instantiatingPauseUIQuery = GetEntityQuery(ComponentType.ReadOnly<PauseUI>(), ComponentType.ReadOnly<Prefab>());
            pauseUIQuery = GetEntityQuery(ComponentType.ReadOnly<PauseUI>());
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc()
            {
                Any = new ComponentType[] {
                    ComponentType.ReadOnly<SceneLoaded>()
                }
            }));
        }
        private void InitGUI(VisualElement ui)
        {
            ui.Q<Button>("Save").clicked += Save;
        }

        private void Save()
        {
            Debug.Log("Saving game");
            saveSystem.Save();
        }
        protected override void OnUpdate()
        {
            Debug.Log($"In Pause UI SYSTEM");
            var input = inputSystem.Input;
            if (input.UI.TogglePauseMenu.WasPressedThisFrame())
            {
                if (pauseUIQuery.CalculateEntityCount() == 0)
                {
                    var toInstanciate = instantiatingPauseUIQuery.GetSingletonEntity();
                    var uiEntity = EntityManager.Instantiate(toInstanciate);
                    SetPause(true);
                    var uiDocument = EntityManager.GetComponentObject<UIDocument>(uiEntity);
                    /*   EntityManager.AddComponent<Disabled>(toDisableQuery); */
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
            World.GetExistingSystem<MouvementSystemGroup>().Enabled = paused;
            World.GetExistingSystem<Unity.Animation.ProcessDefaultAnimationGraph>().Enabled = paused;
            World.GetExistingSystem<CombatSystemGroup>().Enabled = paused;
        }
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class MainGameUISystem : SystemBase
    {

        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery uiDocumentQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            uiDocumentQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                None = new ComponentType[] {
                    ComponentType.ReadOnly<Initialized>()
                },
                All = new ComponentType[]{
                    ComponentType.ReadOnly<UIReady>(), ComponentType.ReadOnly<NewGameUI>(),typeof(UIDocument)
                }
            });
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]{
                    ComponentType.ReadOnly<UIReady>(), ComponentType.ReadOnly<NewGameUI>(),typeof(UIDocument)
                }
            }));
        }
        protected void NewGame(Entity e)
        {
            EntityManager.AddComponent<TriggerNewGame>(e);
            Debug.Log($"Click on new game");
        }
        protected void LoadSave(Entity e)
        {
            EntityManager.AddComponent<TriggerLoad>(e);
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
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        private void InitNewGameUI()
        {
            var uiDocumentEntity = uiDocumentQuery.GetSingletonEntity();
            var uiDocument = EntityManager.GetComponentObject<UIDocument>(uiDocumentEntity);
            Debug.Log($"Set up main game ui");
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
                em.Instantiate(e);
                em.RemoveComponent<AutoInstantiateUI>(e);
            }).WithStructuralChanges().WithoutBurst().Run();
        }
    }

}