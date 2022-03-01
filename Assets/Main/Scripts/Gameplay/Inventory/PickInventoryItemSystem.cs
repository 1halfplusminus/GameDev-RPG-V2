

using RPG.Combat;
using RPG.Control;
using Unity.Entities;
using UnityEngine;

namespace RPG.Gameplay.Inventory
{
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public class PickInventoryItemSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery collidWithPickableweaponQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();

            Entities
            .WithNone<Picked>()
            .ForEach((int entityInQueryIndex,
            Entity e,
            in CollidWithPlayer collidWithPlayer,
            in ItemDefinitionReference itemDefinitionReference) =>
            {

                if (!HasComponent<Inventory>(collidWithPlayer.Entity)) { return; }
                Debug.Log("Collid with player with inventory");
                cb.AddComponent(collidWithPlayer.Entity, new AddItem
                {
                    ItemDefinitionAsset = itemDefinitionReference.ItemDefinitionAssetBlob,
                    ItemDefinition = itemDefinitionReference.AssetEntity,
                    ItemPrefab = itemDefinitionReference.ItemPrefab
                });
            }).Schedule();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }
}