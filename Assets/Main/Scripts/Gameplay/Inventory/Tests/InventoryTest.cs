using NUnit.Framework;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using RPG.Gameplay.Inventory;
using UnityEngine.AddressableAssets;
using System;

namespace RPG.Test
{

    public class InventoryTest
    {
        [Test]
        public void TestInventoryAuthoringConversion()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;
            var exampleInventoryHandle = Addressables.LoadAssetAsync<GameObject>("Gameplay/Inventory/Prefabs/Example Inventory.prefab");
            exampleInventoryHandle.WaitForCompletion();
            var exampleInventoryGO = (GameObject)exampleInventoryHandle.Result;
            var convertToEntitySystem = world.GetOrCreateSystem<ConvertToEntitySystem>();
            var conversionSetting = GameObjectConversionSettings.FromWorld(world, convertToEntitySystem.BlobAssetStore);
            var inventoryEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(exampleInventoryGO, conversionSetting);
            var itemsBuffer = em.GetBuffer<InventoryItem>(inventoryEntity);
            Assert.IsTrue(itemsBuffer.Length >= 1);
            Assert.IsTrue(itemsBuffer[2].Index == 2);
            Assert.IsFalse(itemsBuffer[0].IsEmpty);
            Assert.IsTrue(itemsBuffer[0].Item != Entity.Null);
            Assert.IsFalse(itemsBuffer[1].IsEmpty);
            Assert.IsFalse(String.IsNullOrEmpty(itemsBuffer[2].ItemDefinitionAsset.Value.GUID.ToString()));
            Assert.IsTrue(itemsBuffer[2].ItemDefinitionAsset.Value.GUID != itemsBuffer[1].ItemDefinitionAsset.Value.GUID);
        }
        [Test]
        public void TestItemDefinitionConversion()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityMananger = world.EntityManager;
            var itemDefinitionAssetHandle = Addressables.LoadAssetAsync<ItemDefinitionAsset>("Assets/Main/Scripts/Gameplay/Inventory/Tests/Test Item 1.asset");
            itemDefinitionAssetHandle.WaitForCompletion();
            var itemDefinitionAsset = itemDefinitionAssetHandle.Result;
            var gameObject = new GameObject();
            var itemDefinitionAssetAuthoring = gameObject.AddComponent<InventoryItemAuthoring>();
            itemDefinitionAssetAuthoring.ItemDefinitionAsset = itemDefinitionAsset;
            var convertToEntitySystem = world.GetOrCreateSystem<ConvertToEntitySystem>();
            var conversionSetting = GameObjectConversionSettings.FromWorld(world, convertToEntitySystem.BlobAssetStore);
            var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(gameObject, conversionSetting);

            var hash = new UnityEngine.Hash128();
            hash.Append(itemDefinitionAsset.ID);
            BlobAssetReference<ItemDefinitionAssetBlob> itemDefinitionBlobAsset;
            convertToEntitySystem.BlobAssetStore.TryGet(hash, out itemDefinitionBlobAsset);
            var itemText = entityMananger.GetSharedComponentData<ItemTexture>(entity);
            Debug.Log($"Item name : {itemDefinitionBlobAsset.Value.FriendlyName.ToString()}");
            Assert.IsTrue(itemDefinitionBlobAsset.IsCreated);

        }

        [Test]
        public void TestNeighborIndex()
        {

            var inventory = new Inventory { Height = 8, Width = 8 };
            var inventoryGUI = new InventoryGUI { };
            inventoryGUI.Init(inventory, 150);
            using var index0Neightbor = inventoryGUI.GetNeighborsIndex(0);
            Assert.AreEqual(index0Neightbor.ToArray(), new int[] { 1, 8, 9 });
            inventoryGUI.ResizeSlot(0, 2);
            using var index0NeightborResized = inventoryGUI.GetNeighborsIndex(0);
            Assert.AreEqual(index0NeightborResized.ToArray(), new int[] { 1, 8, 9 });
            using var index11Neightbor = inventoryGUI.GetNeighborsIndex(new int2(1, 1));
            Assert.IsTrue(index11Neightbor.Length == 8);
        }
        [Test]
        public void TestIsVisible()
        {

            var inventory = new Inventory { Height = 1, Width = 3 };
            var inventoryGUI = new InventoryGUI { };
            inventoryGUI.Init(inventory, 150f);
            var visibilities = inventoryGUI.CalculeOverlapse();
            for (int i = 0; i < visibilities.Length; i++)
            {
                Assert.IsFalse(visibilities[i]);
            }
            inventoryGUI.ResizeSlot(0, 2);
            visibilities = inventoryGUI.CalculeOverlapse();
            Assert.IsTrue(visibilities[1]);
            Assert.IsFalse(visibilities[2]);
            inventoryGUI.ResizeSlot(0, 3);
            visibilities = inventoryGUI.CalculeOverlapse();
            Assert.IsTrue(visibilities[1]);
            Assert.IsTrue(visibilities[2]);
            inventoryGUI.Dispose();
            // inventoryGUI.ResizeSlot(0, 3);
            // visibilities = inventoryGUI.CalculeOverlapse();
            // Assert.IsTrue(visibilities[1]);
            // Assert.IsTrue(visibilities[2]);
            // Assert.IsFalse(visibilities[3]);
            // visibilities.Dispose();
            // inventoryGUI.Dispose();
        }
    }

}
