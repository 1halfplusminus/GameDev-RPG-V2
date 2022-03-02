using RPG.Combat;
using RPG.Core;
using RPG.Stats;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
namespace RPG.Gameplay.Inventory
{

    [ExecuteAlways]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public class CommonInventorySystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery usedItemsQuery;

        EntityQuery itemToRemoveQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            usedItemsQuery = GetEntityQuery(typeof(UsedItem));
            RequireForUpdate(usedItemsQuery);
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            cb.RemoveComponent<UsedItem>(usedItemsQuery);
            var inventoryGUIByEntity = new NativeMultiHashMap<Entity, int>(itemToRemoveQuery.CalculateEntityCount(), Allocator.TempJob);
            var entities = itemToRemoveQuery.ToEntityArray(Allocator.Temp);
            var inventories = itemToRemoveQuery.ToComponentDataArray<Inventory>(Allocator.Temp);
            var usedItems = itemToRemoveQuery.ToComponentDataArray<UsedItem>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var items = EntityManager.GetBuffer<InventoryItem>(entities[i]);
                var inventoryGUI = new InventoryGUI();
                var usedItem = usedItems[i];
                var item = items[usedItem.Index];
                inventoryGUI.Init(inventories[i], 1f);
                var dimension = item.ItemDefinitionAsset.Value.Dimension;
                inventoryGUI.ResizeSlot(usedItem.Index, dimension);
                var slots = inventoryGUI.GetSlots(usedItem.Index);
                for (int j = 0; j < slots.Length; j++)
                {
                    inventoryGUIByEntity.Add(entities[i], slots[j]);
                }

                inventoryGUI.Dispose();
            }
            entities.Dispose();
            inventories.Dispose();
            Entities
            .WithReadOnly(inventoryGUIByEntity)
            .WithDisposeOnCompletion(inventoryGUIByEntity)
            .WithStoreEntityQueryInField(ref itemToRemoveQuery)
            .ForEach((Entity e, ref DynamicBuffer<InventoryItem> items, in UsedItem usedItem, in Inventory inventory) =>
            {
                if (HasComponent<RemoveFromInventoryWhenUsed>(usedItem.Item))
                {
                    var slots = inventoryGUIByEntity.GetValuesForKey(e);
                    var item = items[usedItem.Index];
                    foreach (var slot in slots)
                    {
                        var emptyItem = InventoryItem.Empty;
                        emptyItem.Index = slot;
                        items[slot] = emptyItem;
                    }

                }
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        public static void Log(string message)
        {
            Debug.Log(message);
        }
    }
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public class ItemInventorySystem : SystemBase
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
            .ForEach((in HealingAudio healingAudio, in UsedItem usedItem) =>
            {
                var user = usedItem.UsedBy;
                var location = GetComponent<Translation>(user);
                var audioSourceEntity = EntityManager.GetComponentObject<AudioSource>(healingAudio.Entity);
                audioSourceEntity.transform.position = location.Value;
                audioSourceEntity.Play();
                Debug.Log("Play healing audio");
            })
            .WithoutBurst()
            .Run();

            Entities
           .WithAll<HealthPickup>()
           .ForEach((int entityInQueryIndex, Entity e, in UsedItem item, in RestaureHealthPercent restaureHealthPercent) =>
           {
               var target = item.UsedBy;
               if (HasComponent<Health>(target) && HasComponent<BaseStats>(target))
               {
                   var basestats = GetComponent<BaseStats>(target);
                   var health = GetComponent<Health>(target);
                   restaureHealthPercent.RestaureHealth(ref health, basestats);
                   cbp.AddComponent(entityInQueryIndex, target, health);
                   cbp.RemoveComponent<UsedItem>(entityInQueryIndex, e);
                   Log(e, health.Value);
               }
           }).ScheduleParallel();


        }

        private static void Log(Entity e, float newHealth)
        {
            Debug.Log($"Restaure {newHealth} health for ${e.Index}");
        }
    }
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public class WeaponInventorySystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery weaponAssetQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            weaponAssetQuery = GetEntityQuery(typeof(WeaponAssetData));
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            var weaponAssetDatas = weaponAssetQuery.ToComponentDataArray<WeaponAssetData>(Allocator.TempJob);
            var weaponEntities = weaponAssetQuery.ToEntityArray(Allocator.TempJob);
            Entities
            .WithReadOnly(weaponAssetDatas)
            .WithReadOnly(weaponEntities)
            .WithDisposeOnCompletion(weaponAssetDatas)
            .WithDisposeOnCompletion(weaponEntities)
            .ForEach((int entityInQueryIndex, Entity e, in WeaponAssetReference weaponAssetReference, in UsedItem usedItem) =>
            {
                for (int i = 0; i < weaponAssetDatas.Length; i++)
                {
                    var weaponAssetData = weaponAssetDatas[i];
                    if (weaponAssetData.Weapon.Value.Weapon.GUID == weaponAssetReference.Address)
                    {
                        cbp.AddComponent(entityInQueryIndex, usedItem.UsedBy, new Equip { Equipable = weaponEntities[i], SocketType = weaponAssetData.Weapon.Value.Weapon.SocketType });
                        break;
                    }
                }

            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }
}