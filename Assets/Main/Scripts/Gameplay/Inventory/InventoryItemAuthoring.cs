using UnityEngine;
using Unity.Entities;

namespace RPG.Gameplay.Inventory
{

    public class InventoryItemAuthoring : MonoBehaviour
    {
        public ItemDefinitionAsset ItemDefinitionAsset;

        public GameObject Item;


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
