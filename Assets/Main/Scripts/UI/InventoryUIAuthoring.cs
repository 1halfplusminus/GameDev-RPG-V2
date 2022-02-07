

using Unity.Entities;
using RPG.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;
using RPG.Stats;

namespace RPG.UI
{
    public class InventoryUIController : Object, IUIController, IComponentData
    {
        Label Level;
        Label ExperiencePoint;
        VisualElement ExperiencePointBar;
        public void Init(VisualElement root)
        {
            Level = root.Q<Label>("Level_Value");
            ExperiencePoint = root.Q<Label>("ExperiencePoint");
            ExperiencePointBar = root.Q<VisualElement>("Bar_Inner");
        }

        public void SetLevel(int level)
        {
            Level.text = level.ToString();
        }

        public void SetExperiencePoint(int currentPoint, int toLevel)
        {
            var lenght = new StyleLength(new Length(currentPoint * 100 / toLevel, LengthUnit.Percent));
            ExperiencePoint.text = $"{currentPoint}/{toLevel}";
            ExperiencePointBar.style.width = lenght;
        }
    }
    [GenerateAuthoringComponent]
    public struct InventoryFactory : IComponentData
    {
        public Entity Prefab;
    }
    public struct InventoryUIInstance : IComponentData
    {
        public Entity Entity;
    }

    public struct InventoryParent : IComponentData
    {
        public Entity Entity;

    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class InventoryUISystem : SystemBase
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
            var cbp = cb.AsParallelWriter();
            Entities
            .WithNone<InventoryUIInstance>()
            .ForEach((int entityInQueryIndex, Entity e, in InventoryFactory inventoryPrefab, in GameplayInput input) =>
            {
                if (input.OpenInventoryPressedThisFrame)
                {

                    var instance = cbp.Instantiate(entityInQueryIndex, inventoryPrefab.Prefab);
                    cbp.AddComponent(entityInQueryIndex, e, new InventoryUIInstance { Entity = instance });
                    cbp.AddComponent(entityInQueryIndex, instance, new InventoryParent { Entity = e });
                }
            }).ScheduleParallel();

            Entities
            .ForEach((int entityInQueryIndex, Entity e, in GameplayInput gameplayInput, in InventoryUIInstance inventoryUiInstance) =>
            {
                Debug.Log($"Close Inventory {gameplayInput.CloseInventoryPressedThisFrame}");
                if (gameplayInput.OpenInventoryPressedThisFrame)
                {
                    cbp.RemoveComponent<InventoryUIInstance>(entityInQueryIndex, e);
                    cbp.DestroyEntity(entityInQueryIndex, inventoryUiInstance.Entity);
                }
            }).ScheduleParallel();

            Entities
            .WithNone<InventoryUIController>()
            .WithAll<InventoryUI, UIReady>().ForEach((Entity e, UIDocument document, InventoryParent inventoryParent) =>
            {
                var controller = new InventoryUIController();
                controller.Init(document.rootVisualElement);
                cb.AddComponent(e, controller);
                if (inventoryParent.Entity != Entity.Null)
                {
                    cb.AddComponent(inventoryParent.Entity, controller);
                }
            })
            .WithoutBurst()
            .Run();

            Entities
            .ForEach((Entity e, InventoryUIController uIController, BaseStats baseStats, ExperiencePoint experiencePoint) =>
            {
                uIController.SetLevel(baseStats.Level);
                uIController.SetExperiencePoint((int)experiencePoint.Value, (int)baseStats.ProgressionAsset.Value.GetStat(Stats.Stats.ExperiencePointToLevelUp, baseStats.Level));
            })
            .WithoutBurst()
            .Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}