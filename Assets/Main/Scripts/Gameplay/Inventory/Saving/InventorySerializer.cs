

using System;
using System.Runtime.InteropServices;
using RPG.Core;
using RPG.Saving;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RPG.Gameplay.Inventory
{
    public struct InventorySerializer : ISerializer
    {
        public EntityQueryDesc GetEntityQueryDesc()
        {
            return new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(Inventory)
                }
            };
        }
        object[] referencedObjects;
        public object Serialize(EntityManager em, Entity e)
        {
            var world = new World("Serialize World", WorldFlags.Shadow);
            var serializedWorldEm = world.EntityManager;
            var items = em.GetBuffer<InventoryItem>(e).ToNativeArray(Allocator.Temp);
            var inventoryEntity = serializedWorldEm.CreateEntity();
            serializedWorldEm.AddBuffer<InventoryItem>(inventoryEntity);
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (!item.IsEmpty && em.Exists(item.ItemPrefab))
                {
                    if (em.HasComponent<Addressable>(item.ItemPrefab))
                    {
                        var address = em.GetComponentData<Addressable>(item.ItemPrefab);
                        var itemEntity = serializedWorldEm.CreateEntity();
                        item.ItemPrefab = itemEntity;
                        serializedWorldEm.AddComponentData(itemEntity, address);
                    }

                }
                var serializedItem = serializedWorldEm.GetBuffer<InventoryItem>(inventoryEntity);
                serializedItem.Add(item);
            }
            items.Dispose();
            var memoryWriter = new MemoryBinaryWriter();
            SerializeUtility.SerializeWorld(serializedWorldEm, memoryWriter, out referencedObjects);

            byte[] arr = new byte[memoryWriter.Length];
            unsafe
            {
                Marshal.Copy((IntPtr)memoryWriter.Data, arr, 0, memoryWriter.Length);
            }
            world.Dispose();
            return arr;
        }

        public void UnSerialize(EntityManager em, Entity e, object state)
        {
            var inventory = em.GetComponentData<Inventory>(e);
            var inventoryGUI = InventoryGUI.Build(inventory, em.GetBuffer<InventoryItem>(e).AsNativeArray());
            var world = new World("Deserialize World", WorldFlags.Shadow);
            unsafe
            {
                byte[] bytes = (byte[])state;
                fixed (byte* ptr = &bytes[0])
                {
                    var reader = new MemoryBinaryReader(ptr);
                    SerializeUtility.DeserializeWorld(world.EntityManager.BeginExclusiveEntityTransaction(), reader, referencedObjects);
                    world.EntityManager.EndExclusiveEntityTransaction();
                    var inventoryQuery = world.EntityManager.CreateEntityQuery(typeof(InventoryItem));
                    var inventoryEntity = inventoryQuery.GetSingletonEntity();
                    var restoredItems = world.EntityManager.GetBuffer<InventoryItem>(inventoryEntity);
                    for (int i = 0; i < restoredItems.Length; i++)
                    {
                        var restoredItem = restoredItems[i];
                        var itemPrefabEntity = restoredItems[i].ItemPrefab;
                        if (world.EntityManager.Exists(itemPrefabEntity))
                        {
                            var convertToEntitySystem = em.World.GetOrCreateSystem<ConvertToEntitySystem>();
                            var convertSetting = GameObjectConversionSettings.FromWorld(em.World, convertToEntitySystem.BlobAssetStore);
                            var addressable = world.EntityManager.GetComponentData<Addressable>(itemPrefabEntity);
                            var assetRef = new AssetReference(addressable.Address.ToString());
                            var resultHandle = assetRef.LoadAssetAsync<GameObject>();
                            resultHandle.WaitForCompletion();
                            var itemEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(resultHandle.Result, convertSetting);
                            var itemDefinition = em.GetComponentData<ItemDefinitionReference>(itemEntity);
                            restoredItem.ItemPrefab = itemEntity;
                            restoredItem.ItemDefinition = itemDefinition.AssetEntity;
                            restoredItem.ItemDefinitionAsset = itemDefinition.ItemDefinitionAssetBlob;
                            var items = em.GetBuffer<InventoryItem>(e);
                            inventoryGUI.Insert(i, restoredItem, items.AsNativeArray());
                            Debug.Log("Restore item at address " + addressable.Address);
                            assetRef.ReleaseInstance(resultHandle.Result);
                            assetRef.ReleaseAsset();

                        }

                    }
                }

            }
            world.Dispose();
            inventoryGUI.Dispose();

        }
    }
}