using RPG.Combat;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace RPG.Gameplay.Inventory
{

    [ExecuteAlways]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public class CommonInventorySystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery usedItemsQuery;
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
            Entities
            .ForEach((Entity e, ref DynamicBuffer<InventoryItem> items, in UsedItem usedItem) =>
            {
                if (HasComponent<RemoveFromInventoryWhenUsed>(usedItem.Item))
                {
                    var emptyItem = InventoryItem.Empty;
                    emptyItem.Index = usedItem.Index;
                    items[usedItem.Index] = emptyItem;
                }

            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        public static void Log(string message)
        {
            Debug.Log(message);
        }
    }
    [ExecuteAlways]
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
                        Debug.Log($"Equip Weapon From Inventory {weaponAssetData.Weapon.Value.Weapon.GUID}");
                        break;
                    }
                }

            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        private static void LogFixedString(FixedString64 message)
        {
            Debug.Log($"Equip Weapon from inventory {message}");
        }
        private static void Log(string message)
        {
            Debug.Log($"message: {message}");
        }
    }
}