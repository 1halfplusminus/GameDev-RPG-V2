

using System;
using RPG.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RPG.Gameplay.Inventory
{
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
            Entities.ForEach((InventoryAuthoring inventoryAuthoring) =>
            {
                foreach (var item in inventoryAuthoring.Items)
                {
                    item.ReleaseAsset();
                    AsyncOperationHandle<GameObject> handle = item.LoadAssetAsync<GameObject>();
                    var r = handle.WaitForCompletion();
                    DeclareReferencedPrefab(r);
                }
            });
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
                        var itemEntity = conversionSystem.GetPrimaryEntity(itemAuthoring.ItemDefinitionAsset);
                        var itemDefinitionBlobAsset = conversionSystem.BlobAssetStore.GetItemDefinitionAssetBlob(itemAuthoring.ItemDefinitionAsset);
                        if (itemDefinitionBlobAsset.IsCreated)
                        {
                            inventoryGUI.Add
                            (
                                new InventoryItem { ItemDefinitionAsset = itemDefinitionBlobAsset, ItemDefinition = itemEntity, },
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
            Entities.ForEach((Entity entity, ref DynamicBuffer<InventoryItem> items, in Inventory inventory, in AddItem addItem) =>
            {
                var inventoryGUI = new InventoryGUI { };
                inventoryGUI.Init(inventory, 1f);
                var iventoryItem = new InventoryItem
                {
                    ItemDefinitionAsset = addItem.ItemDefinitionAsset,
                    ItemDefinition = addItem.ItemDefinition
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