

using System;
using System.Collections.Generic;
using RPG.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RPG.Gameplay.Inventory
{
    public struct UsedItem : IComponentData
    {
        public int Index;
        public Entity UsedBy;

        public Entity Item;
    }
    [Serializable]
    public class InventoryItemAuthoringReference : ComponentReference<InventoryItemAuthoring>
    {
        public InventoryItemAuthoringReference(string guid) : base(guid)
        {
        }
    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class InventoryDeclareConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            var query = EntityManager.CreateEntityQuery(typeof(InventoryAuthoring));
            var inventoryAuthorings = query.ToComponentArray<InventoryAuthoring>();
            foreach (var inventoryAuthoring in inventoryAuthorings)
            {
                foreach (var item in inventoryAuthoring.Items)
                {
                    item.ReleaseAsset();
                    AsyncOperationHandle<GameObject> handle = item.LoadAsset<GameObject>();
                    handle.Completed += (r) =>
                    {
                        DeclareReferencedPrefab(r.Result);
                    };
                    handle.WaitForCompletion();
                }
            }
        }
    }

    public class InventoryAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField]
        int2 Dimension;

        [SerializeField]
        public InventoryItemAuthoringReference[] Items;



        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var inventory = new Inventory { Height = Dimension.y, Width = Dimension.x };
            if (inventory.Size > 0)
            {
                dstManager.AddComponentData(entity, inventory);
                var itemsBuffer = dstManager.AddBuffer<InventoryItem>(entity);
                itemsBuffer.Capacity = inventory.Size;
                itemsBuffer.ResizeUninitialized(inventory.Size);
                var inventoryGUI = InventoryGUI.Build(inventory, itemsBuffer.AsNativeArray());
                foreach (var itemHandle in Items)
                {
                    var itemAuthoringGO = (GameObject)itemHandle.OperationHandle.Result;
                    var itemAuthoring = itemAuthoringGO.GetComponent<InventoryItemAuthoring>();
                    if (itemAuthoring != null)
                    {
                        var itemPrefab = conversionSystem.GetPrimaryEntity(itemAuthoring.Item);
                        var itemEntity = conversionSystem.GetPrimaryEntity(itemAuthoring.ItemDefinitionAsset);
                        var itemDefinitionBlobAsset = conversionSystem.BlobAssetStore.GetItemDefinitionAssetBlob(itemAuthoring.ItemDefinitionAsset);
                        if (itemDefinitionBlobAsset.IsCreated)
                        {
                            inventoryGUI.Add
                            (
                                new InventoryItem
                                {
                                    ItemDefinitionAsset = itemDefinitionBlobAsset,
                                    ItemDefinition = itemEntity,
                                    ItemPrefab = itemPrefab,
                                },
                                itemsBuffer.AsNativeArray()
                            );
                        }
                    }
                }
                inventoryGUI.Dispose();
            }

        }
    }
    public struct AddItem : IComponentData
    {
        public BlobAssetReference<ItemDefinitionAssetBlob> ItemDefinitionAsset;
        public Entity ItemDefinition;

        public Entity ItemPrefab;
    }

    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public class InventorySystem : SystemBase
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
            Entities.ForEach((Entity entity,
            ref DynamicBuffer<InventoryItem> items,
            in Inventory inventory,
            in AddItem addItem,
            in SceneSection sceneSection,
            in SceneTag sceneTag) =>
            {
                var inventoryGUI = new InventoryGUI { Created = false };
                inventoryGUI.Init(inventory, 1f);
                cb.AddSharedComponent(addItem.ItemDefinition, sceneSection);
                cb.AddSharedComponent(addItem.ItemPrefab, sceneSection);

                cb.AddSharedComponent(addItem.ItemPrefab, sceneTag);
                cb.AddSharedComponent(addItem.ItemDefinition, sceneTag);
                var iventoryItem = new InventoryItem
                {
                    ItemDefinitionAsset = addItem.ItemDefinitionAsset,
                    ItemDefinition = addItem.ItemDefinition,
                    ItemPrefab = addItem.ItemPrefab
                };
                inventoryGUI.Add(iventoryItem, items.AsNativeArray());
                inventoryGUI.Dispose();
                cb.RemoveComponent<AddItem>(entity);
            })
            .WithoutBurst()
            .Run();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}