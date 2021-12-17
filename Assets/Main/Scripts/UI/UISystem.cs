using RPG.Core;
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
    public class MainGameUISystem : SystemBase
    {
        EntityQuery newGameQuery;
        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery uiDocumentQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            newGameQuery = GetEntityQuery(ComponentType.ReadOnly<NewGame>());
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
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
            if (newGameQuery.CalculateEntityCount() > 0)
            {

                /*   Entities
                  .WithAll<UIReady>()
                  .WithAny<NewGame>()
                  .ForEach((int entityInQueryIndex, Entity e, UIDocument uiDocument) =>
                  {
                      Debug.Log($"Hide main UI");
                      ec.DestroyEntity(e);

                  }).WithoutBurst().Run(); */
            }
            Entities
            .WithAll<UIReady>()
            .WithAny<TriggeredSceneLoaded>()
            .ForEach((int entityInQueryIndex, Entity e, UIDocument uiDocument) =>
            {
                Debug.Log($"Hide main UI");
                ec.DestroyEntity(e);
            }).WithoutBurst().Run();
            Entities
            .WithStoreEntityQueryInField(ref uiDocumentQuery)
            .WithNone<Initialized>()
            .WithAll<UIReady, NewGameUI>()
            .ForEach((Entity e, UIDocument uiDocument) =>
            {

                ec.AddComponent<Initialized>(e);
            })
            .WithoutBurst().Run();
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