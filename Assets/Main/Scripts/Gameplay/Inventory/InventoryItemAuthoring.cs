using UnityEngine;
using Unity.Entities;

namespace RPG.Gameplay.Inventory
{

    public class InventoryItemAuthoring : MonoBehaviour
    {
        public ItemDefinitionAsset ItemDefinitionAsset;

        public GameObject Item;

        // public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        // {
        //     if (ItemDefinitionAsset != null)
        //     {
        //         conversionSystem.DeclareAssetDependency(gameObject, ItemDefinitionAsset);
        //         var blobAssetReference = conversionSystem.BlobAssetStore.GetItemDefinitionAssetBlob(ItemDefinitionAsset);
        //         var assetEntity = conversionSystem.GetPrimaryEntity(ItemDefinitionAsset);
        //         var itemPrefab = conversionSystem.GetPrimaryEntity(Item);
        //         dstManager.AddComponentData(entity, new ItemDefinitionReference { ItemDefinitionAssetBlob = blobAssetReference, AssetEntity = assetEntity, ItemPrefab = itemPrefab });
        //     }

        // }

    }
    class InventoryItemConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((InventoryItemAuthoring itemDefinitionAssetAuthoring) =>
            {
                var entity = GetPrimaryEntity(itemDefinitionAssetAuthoring);
                var blobAssetReference = BlobAssetStore.GetItemDefinitionAssetBlob(itemDefinitionAssetAuthoring.ItemDefinitionAsset);
                var assetEntity = GetPrimaryEntity(itemDefinitionAssetAuthoring.ItemDefinitionAsset);
                var itemPrefab = GetPrimaryEntity(itemDefinitionAssetAuthoring.Item);
                DstEntityManager.AddComponentData(entity, new ItemDefinitionReference { ItemDefinitionAssetBlob = blobAssetReference, AssetEntity = assetEntity, ItemPrefab = itemPrefab });
            });
        }
    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    class ItemDefinitionAssetDeclare : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((InventoryItemAuthoring itemDefinitionAssetAuthoring) =>
            {
                if (itemDefinitionAssetAuthoring.Item == null)
                {
                    itemDefinitionAssetAuthoring.Item = itemDefinitionAssetAuthoring.gameObject;
                }
                DeclareReferencedPrefab(itemDefinitionAssetAuthoring.Item);
                DeclareReferencedAsset(itemDefinitionAssetAuthoring.ItemDefinitionAsset);
                DeclareAssetDependency(itemDefinitionAssetAuthoring.gameObject, itemDefinitionAssetAuthoring.ItemDefinitionAsset);
                DeclareAssetDependency(itemDefinitionAssetAuthoring.Item, itemDefinitionAssetAuthoring.ItemDefinitionAsset);
            });
        }
    }
}
