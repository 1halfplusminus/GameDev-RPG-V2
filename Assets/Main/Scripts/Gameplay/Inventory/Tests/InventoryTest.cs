using NUnit.Framework;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using RPG.Gameplay.Inventory;
using UnityEngine.AddressableAssets;
using System;
using Unity.Physics;

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
            Assert.IsTrue(itemsBuffer[0].ItemDefinition != Entity.Null);
            Assert.IsFalse(itemsBuffer[1].IsEmpty);
            Assert.IsFalse(String.IsNullOrEmpty(itemsBuffer[2].ItemDefinitionAsset.Value.GUID.ToString()));
            Assert.IsTrue(itemsBuffer[2].ItemDefinitionAsset.Value.GUID.ToString() != itemsBuffer[1].ItemDefinitionAsset.Value.GUID.ToString());
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
        public void TestIsVisible()
        {

            var inventory = new Inventory { Height = 1, Width = 3 };
            var inventoryGUI = new InventoryGUI { };
            inventoryGUI.Init(inventory, 150f);
            var simulation = new Simulation();
            var visibilities = inventoryGUI.CalculeOverlapse(simulation);
            for (int i = 0; i < visibilities.Length; i++)
            {
                Assert.IsFalse(visibilities[i]);
            }
            inventoryGUI.ResizeSlot(0, 2);
            visibilities = inventoryGUI.CalculeOverlapse(simulation);
            Assert.IsTrue(visibilities[1]);
            Assert.IsFalse(visibilities[2]);
            inventoryGUI.ResizeSlot(0, 3);
            visibilities = inventoryGUI.CalculeOverlapse(simulation);
            Assert.IsTrue(visibilities[1]);
            Assert.IsTrue(visibilities[2]);
            simulation.Dispose();
            inventoryGUI.Dispose();
        }
    }

}
