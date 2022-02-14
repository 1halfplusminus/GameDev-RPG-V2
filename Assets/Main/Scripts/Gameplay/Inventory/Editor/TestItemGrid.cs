using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AddressableAssets;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using RPG.UI;
using Unity.Jobs;

namespace RPG.Gameplay.Inventory
{

    public class TestItemGrid : ItemGrid
    {

        public new class UxmlFactory : UxmlFactory<TestItemGrid, TestItemGrid.UxmlTraits>
        {
            public override string uxmlName => base.uxmlName;

            public override string uxmlNamespace => base.uxmlNamespace;

            public override string uxmlQualifiedName => base.uxmlQualifiedName;

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription => base.uxmlAttributesDescription;

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription => base.uxmlChildElementsDescription;

            public override string substituteForTypeName => base.substituteForTypeName;

            public override string substituteForTypeNamespace => base.substituteForTypeNamespace;

            public override string substituteForTypeQualifiedName => base.substituteForTypeQualifiedName;

            public override bool AcceptsAttributeBag(IUxmlAttributes bag, CreationContext cc)
            {
                return base.AcceptsAttributeBag(bag, cc);
            }

            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {

                var result = base.Create(bag, cc);
                var root = cc.target;
                // cc.slotInsertionPoints.TryGetValue("Container", out var parent);
                // if (parent != null)
                // {
                //     var world = World.DefaultGameObjectInjectionWorld;
                //     var controller = new InventoryUIController();
                //     controller.Init(result);
                //     Debug.Log($"Here {parent.name} ");
                // }

                return result;
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string ToString()
            {
                return base.ToString();
            }
        }
        Entity inventoryEntityPrefab;
        Entity inventoryEntity;
        public TestItemGrid()
        {

            AddToClassList("inventory-grid");
            name = "Grid";
            RegisterCallback<AttachToPanelEvent>((e) =>
            {
                var inventoryHandle = Addressables.LoadAssetAsync<GameObject>("Gameplay/Inventory/Prefabs/Example Inventory.prefab");
                inventoryHandle.Completed += (r) =>
                {
                    var inventoryGO = r.Result;
                    var world = World.DefaultGameObjectInjectionWorld;
                    if (world != null)
                    {
                        var em = world.EntityManager;
                        var convertToEntitySystem = world.GetOrCreateSystem<ConvertToEntitySystem>();
                        var convertionSettings = GameObjectConversionSettings.FromWorld(world, convertToEntitySystem.BlobAssetStore);
                        inventoryEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(inventoryGO, convertionSettings);
                        inventoryEntity = em.Instantiate(inventoryEntityPrefab);
                        var root = GetFirstAncestorOfType<InventoryRootController>();
                        var controller = new InventoryUIController();
                        controller.Init(root);
                        em.AddComponentObject(inventoryEntity, controller);
                        Addressables.Release(inventoryGO);
                    }

                };
                inventoryHandle.WaitForCompletion();
            });
            RegisterCallback<DetachFromPanelEvent>((e) =>
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null && inventoryEntity != Entity.Null)
                {
                    var em = world.EntityManager;
                    em.DestroyEntity(inventoryEntityPrefab);
                    em.DestroyEntity(inventoryEntity);
                }
            });
        }

    }

    // public struct InitConversionSystem : IJob
    // {

    //     public void Execute()
    //     {
    //         while (World.DefaultGameObjectInjectionWorld == null)
    //         {
    //             Debug.Log("Wolrd Is null");
    //         };
    //         var world = World.DefaultGameObjectInjectionWorld;
    //         var em = world.EntityManager;
    //         var convertToEntitySystem = world.GetOrCreateSystem<ConvertToEntitySystem>();
    //         var convertionSettings = GameObjectConversionSettings.FromWorld(world, convertToEntitySystem.BlobAssetStore);
    //         Debug.Log("Here");
    //     }
    // }
}