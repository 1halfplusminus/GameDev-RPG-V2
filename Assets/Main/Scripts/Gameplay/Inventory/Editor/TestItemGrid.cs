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

    struct TestGridTag : IComponentData { }
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
        public TestItemGrid()
        {

            AddToClassList("inventory-grid");
            name = "Grid";
            var world = World.DefaultGameObjectInjectionWorld;

            RegisterCallback<AttachToPanelEvent>((e) =>
            {
                var inventoryHandle = Addressables.LoadAssetAsync<GameObject>("Gameplay/Inventory/Prefabs/Example Inventory.prefab");
                inventoryHandle.Completed += (r) =>
                {
                    var inventoryGO = r.Result;

                    if (world != null)
                    {
                        var em = world.EntityManager;
                        var convertToEntitySystem = world.GetOrCreateSystem<ConvertToEntitySystem>();
                        var convertionSettings = GameObjectConversionSettings.FromWorld(world, convertToEntitySystem.BlobAssetStore);
                        var inventoryEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(inventoryGO, convertionSettings);
                        var inventoryEntity = em.Instantiate(inventoryEntityPrefab);
                        var root = GetFirstAncestorOfType<InventoryRootController>();
                        var controller = new InventoryUIController();
                        controller.Init(root);
                        em.AddComponentObject(inventoryEntity, controller);
                        world.EntityManager.AddComponent<TestGridTag>(inventoryEntity);
                        world.EntityManager.AddComponent<TestGridTag>(inventoryEntityPrefab);
                        Addressables.Release(inventoryGO);
                    }

                };

            });
            RegisterCallback<DetachFromPanelEvent>((e) =>
            {
                if (world != null)
                {
                    world.EntityManager.DestroyEntity(world.EntityManager.CreateEntityQuery(typeof(TestGridTag)));
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