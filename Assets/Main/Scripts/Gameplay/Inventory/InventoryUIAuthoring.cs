

using Unity.Entities;
using RPG.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;
using RPG.Stats;
using RPG.Gameplay.Inventory;
using Unity.Collections;
using System.Collections.Generic;
using System;

namespace RPG.UI
{
    public class InventoryUIController : UnityEngine.Object, IUIController, IComponentData
    {
        Label Level;
        Label ExperiencePoint;
        VisualElement ExperiencePointBar;

        public ItemGrid ItemGrid;
        public void Init(VisualElement root)
        {
            Level = root.Q<Label>("Level_Value");
            ExperiencePoint = root.Q<Label>("ExperiencePoint");
            ExperiencePointBar = root.Q<VisualElement>("Bar_Inner");
            ItemGrid = root.Q<ItemGrid>();
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

        public void MoveItem(NativeArray<InventoryItem> items)
        {
            if (ItemGrid.ItemMoved.MovedThisFrame)
            {
                for (int i = 0; i < ItemGrid.ItemMoved.OldIndex.Length; i++)
                {
                    var oldIndex = ItemGrid.ItemMoved.OldIndex[i];
                    var newIndex = ItemGrid.ItemMoved.NewIndex[i];
                    Debug.Log($"Move {oldIndex} to {newIndex}");
                    var movedValue = items[oldIndex];
                    movedValue.Index = newIndex;
                    items[newIndex] = movedValue;

                    var resetSlot = InventoryItem.Empty;
                    resetSlot.Index = oldIndex;
                    items[oldIndex] = resetSlot;
                }
                ItemGrid.ItemMoved.MovedThisFrame = false;
            }
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
    public struct InventoryInitTag : IComponentData
    {

    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class InventoryUISystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            Debug.Log("Create Inventory UI System");
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnDestroy()
        {
            Entities
            .WithAll<InventoryInitTag>()
            .ForEach((in InventoryUIController controller) =>
            {
                controller.ItemGrid.inventoryGUI.Dispose();
            })
            .WithoutBurst()
            .Run();
            base.OnDestroy();

        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            Entities
            .WithNone<InventoryUIInstance>()
            .ForEach((int entityInQueryIndex, Entity e, in InventoryFactory inventoryPrefab, in GameplayInput input, in Inventory inventory) =>
            {
                if (input.OpenInventoryPressedThisFrame)
                {

                    var instance = cbp.Instantiate(entityInQueryIndex, inventoryPrefab.Prefab);
                    cbp.AddComponent(entityInQueryIndex, instance, inventory);
                    cbp.AddComponent(entityInQueryIndex, e, new InventoryUIInstance { Entity = instance });
                    cbp.AddComponent(entityInQueryIndex, instance, new InventoryParent { Entity = e });
                }
            }).ScheduleParallel();

            Entities
            .ForEach((int entityInQueryIndex,
            Entity e,
            in GameplayInput gameplayInput,
            in InventoryUIInstance inventoryUiInstance
            ) =>
            {
                if (gameplayInput.OpenInventoryPressedThisFrame)
                {
                    cbp.RemoveComponent<InventoryInitTag>(entityInQueryIndex, e);
                    cbp.RemoveComponent<InventoryUIController>(entityInQueryIndex, e);
                    cbp.RemoveComponent<InventoryUIInstance>(entityInQueryIndex, e);
                    cbp.RemoveComponent<InventoryUIController>(entityInQueryIndex, inventoryUiInstance.Entity);
                    cbp.DestroyEntity(entityInQueryIndex, inventoryUiInstance.Entity);
                }
            }).ScheduleParallel();

            Entities
            .WithNone<InventoryUIController>()
            .WithAll<InventoryUI, UIReady>().ForEach((Entity e, in UIDocument document, in InventoryParent inventoryParent) =>
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
            .WithAll<InventoryUIInstance>()
            .WithNone<InventoryInitTag>()
            .ForEach((Entity entity, in InventoryUIController controller, in Inventory inventory) =>
            {
                controller.ItemGrid.InitInventory(inventory);
                cb.AddComponent<InventoryInitTag>(entity);
            })
            .WithoutBurst()
            .Run();

            Entities
            .WithAll<InventoryInitTag>()
            .ForEach((Entity entity, in InventoryUIController controller) =>
            {
                var handle = controller.ItemGrid.inventoryGUI.ScheduleCalculeOverlapse();
                handle.Complete();
            })
            .WithoutBurst()
            .Run();

            Entities
            .WithAll<InventoryInitTag>()
            .ForEach((Entity entity, ref DynamicBuffer<InventoryItem> items, in InventoryUIController controller) =>
            {
                controller.MoveItem(items.AsNativeArray());
                var length = items.Length;
                var itemSlotDescriptions = Array.CreateInstance(typeof(ItemSlotDescription), items.Length);
                var textures = GetSharedComponentTypeHandle<ItemTexture>();
                for (int i = 0; i < items.Length; i++)
                {
                    var itemSlotDescription = new ItemSlotDescription();
                    itemSlotDescription.Dimension = 1;
                    itemSlotDescription.IsEmpty = items[i].IsEmpty;
                    if (items[i].ItemDefinition != Entity.Null && items[i].ItemDefinitionAsset.IsCreated)
                    {
                        var itemTexture = EntityManager.GetSharedComponentData<ItemTexture>(items[i].ItemDefinition);
                        itemSlotDescription.Dimension = items[i].ItemDefinitionAsset.Value.Dimension;
                        itemSlotDescription.GUID = items[i].ItemDefinitionAsset.Value.GUID.ToString();
                        Debug.Log($"Put GUID {itemSlotDescription.GUID} at index {items[i].Index} ");
                        itemSlotDescription.Description = items[i].ItemDefinitionAsset.Value.Description.ToString();
                        itemSlotDescription.FriendlyName = items[i].ItemDefinitionAsset.Value.FriendlyName.ToString();
                        itemSlotDescription.Texture = itemTexture.Texture;
                    }
                    itemSlotDescriptions.SetValue(itemSlotDescription, items[i].Index);
                }

                controller.ItemGrid.DrawItems((ItemSlotDescription[])itemSlotDescriptions);

            })
            .WithoutBurst()
            .Run();

            Entities
            .ForEach((Entity e, in InventoryUIController uIController, in BaseStats baseStats, in ExperiencePoint experiencePoint) =>
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