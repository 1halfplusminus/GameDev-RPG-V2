using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AddressableAssets;
using Unity.Collections;
using Unity.Entities;
using RPG.UI;
using Unity.Jobs;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RPG.Gameplay.Inventory
{

    struct TestGridTag : IComponentData { }

    // [ExecuteAlways]
    // public class TestItemGridSystem : ComponentSystem
    // {
    //     World shadowWorld;
    //     AsyncOperationHandle<GameObject> handle;

    //     NativeArray<Entity> createdEntity;

    //     ConvertToEntitySystem convertToEntitySystem;
    //     protected override void OnCreate()
    //     {
    //         base.OnCreate();
    //         convertToEntitySystem = World.GetOrCreateSystem<ConvertToEntitySystem>();
    //         // shadowWorld = new World("Shadow", WorldFlags.Conversion);
    //         // RequireForUpdate(GetEntityQuery(new EntityQueryDesc()
    //         // {
    //         //     None = new ComponentType[] { typeof(TestGridTag) }
    //         // }));
    //     }

    //     protected override void OnUpdate()
    //     {
    //         if (!handle.IsValid())
    //         {
    //             handle = Addressables.LoadAssetAsync<GameObject>("Gameplay/Inventory/Prefabs/Example Inventory.prefab");
    //         }
    //         if (handle.IsDone && !createdEntity.IsCreated)
    //         {
    //             Debug.Log("Inventory completed");

    //             var inventoryGO = handle.Result;
    //             var convertionSettings = GameObjectConversionSettings.FromWorld(World, convertToEntitySystem.BlobAssetStore);
    //             var inventoryEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(inventoryGO, convertionSettings);
    //             var inventoryEntity = EntityManager.Instantiate(inventoryEntityPrefab);
    //             var entityToCopy = EntityManager.GetAllEntities(Allocator.Temp);
    //             createdEntity = new NativeArray<Entity>(entityToCopy.Length, Allocator.Persistent);
    //             createdEntity[0] = inventoryEntityPrefab;
    //             EntityManager.AddComponent<TestGridTag>(inventoryEntity);
    //             // EntityManager.CopyEntitiesFrom(shadowWorld.EntityManager, entityToCopy, createdEntity);
    //             // entityToCopy.Dispose();
    //         }

    //     }

    //     protected override void OnDestroy()
    //     {
    //         // if (handle.IsDone && handle.IsValid() && handle.Result != null)
    //         // {
    //         //     Addressables.Release(handle.Result);
    //         // }
    //         // shadowWorld.Dispose();
    //         // if (createdEntity.IsCreated)
    //         // {
    //         //     EntityManager.DestroyEntity(createdEntity);
    //         // }
    //         // base.OnDestroy();
    //     }
    // }
    [DisableAutoCreation]
    public class TestGridSystem : ComponentSystem
    {
        AsyncOperationHandle<GameObject> handle;
        World shadowWorld;
        EntityQuery prefabQuery;
        EntityQuery uiQuery;

        EntityQuery uiController;
        NativeArray<Entity> createdEntity;
        protected override void OnCreate()
        {
            base.OnCreate();
            shadowWorld = new World("Shadow", WorldFlags.Shadow);
            prefabQuery = GetEntityQuery(typeof(Prefab), typeof(Inventory));
            uiQuery = GetEntityQuery(typeof(Inventory));
            uiController = GetEntityQuery(typeof(InventoryUIController));
        }
        protected override void OnUpdate()
        {


        }
        protected override void OnDestroy()
        {
            EntityManager.DestroyEntity(uiQuery);
            EntityManager.DestroyEntity(prefabQuery);
            if (createdEntity.IsCreated)
            {
                EntityManager.DestroyEntity(createdEntity);
                createdEntity.Dispose();
            }
            base.OnDestroy();
        }
        public void Convert()
        {
            if (!handle.IsValid())
            {
                handle = Addressables.LoadAssetAsync<GameObject>("Gameplay/Inventory/Prefabs/Example Inventory.prefab");
            }
            if (!handle.IsDone)
            {
                handle.WaitForCompletion();

            }
            if (prefabQuery.CalculateEntityCount() == 0)
            {
                EntityManager.DestroyEntity(uiController);
                EntityManager.DestroyEntity(uiQuery);
                EntityManager.DestroyEntity(prefabQuery);
                ConvertPrefab(World, handle.Result);
                CopyEntities(World, shadowWorld, out createdEntity);
                shadowWorld.EntityManager.DestroyAndResetAllEntities();
                Addressables.Release(handle);
            }


        }
        private static void CopyEntities(World world, World shadowWorld, out NativeArray<Entity> createdEntities)
        {

            var entityToCopy = shadowWorld.EntityManager.GetAllEntities();
            world.EntityManager.MoveEntitiesFrom(out createdEntities, shadowWorld.EntityManager);
            // world.EntityManager.CopyEntitiesFrom(shadowWorld.EntityManager, entityToCopy, createdEntities);
        }
        public Entity CreateInstance()
        {
            if (uiController.CalculateEntityCount() > 0)
            {
                var controllerInstance = uiController.GetSingleton<InventoryUIController>();
                controllerInstance.ItemGrid.inventoryGUI.Dispose();
                Object.DestroyImmediate(controllerInstance);
                EntityManager.DestroyEntity(uiController);
            }
            EntityManager.DestroyEntity(uiQuery);
            Entity instance = CreateInstance(World, prefabQuery.GetSingletonEntity());
            return instance;
        }
        public void DetachController()
        {
            EntityManager.RemoveComponent<InventoryUIController>(uiQuery);
            EntityManager.DestroyEntity(uiQuery);
        }
        private Entity CreateInstance(World world, Entity inventoryEntityPrefab)
        {
            var em = world.EntityManager;
            var inventoryEntity = em.Instantiate(inventoryEntityPrefab);
            return inventoryEntity;
        }

        private static Entity ConvertPrefab(World world, GameObject inventoryGO)
        {
            var em = world.EntityManager;
            var convertToEntitySystem = world.GetOrCreateSystem<ConvertToEntitySystem>();
            var convertionSettings = GameObjectConversionSettings.FromWorld(world, convertToEntitySystem.BlobAssetStore);
            var inventoryEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(inventoryGO, convertionSettings);
            return inventoryEntityPrefab;
        }
    }
    public class TestItemGrid : Button
    {

        public new class UxmlFactory : UxmlFactory<TestItemGrid, TestItemGrid.UxmlTraits>
        {

        }


        public TestItemGrid()
        {
            // AddToClassList("inventory-grid");
            // name = "Grid";
            AddToClassList("my-button");
            text = "Test";
            clicked += () =>
             {
                 var world = World.DefaultGameObjectInjectionWorld;
                 if (world != null)
                 {
                     var em = world.EntityManager;
                     var system = world.GetOrCreateSystem<TestGridSystem>();
                     system.Convert();
                     var root = this.GetFirstAncestorOfType<InventoryRootController>();
                     var instance = system.CreateInstance();
                     var controller = new InventoryUIController();
                     controller.Init(root);
                     em.AddComponentObject(instance, controller);
                     em.AddComponent<InventoryUIInstance>(instance);
                     SetEnabled(false);
                 }
             };

            RegisterCallback((EventCallback<DetachFromPanelEvent>)((e) =>
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null)
                {
                    var em = world.EntityManager;
                    var system = world.GetExistingSystem<TestGridSystem>();
                    if (system != null)
                    {
                        system.DetachController();
                        world.DestroySystem(system);
                    }
                }

            }));


        }

    }

}