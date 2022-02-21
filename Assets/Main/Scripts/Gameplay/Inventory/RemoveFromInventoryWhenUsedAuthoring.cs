using Unity.Entities;
using UnityEngine;
namespace RPG.Gameplay.Inventory
{
    [GenerateAuthoringComponent]
    public struct RemoveFromInventoryWhenUsed : IComponentData { }

    // [ExecuteAlways]
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
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            cb.RemoveComponent<UsedItem>(usedItemsQuery);
            Entities
            .ForEach((ref DynamicBuffer<InventoryItem> items, in UsedItem usedItem) =>
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
}