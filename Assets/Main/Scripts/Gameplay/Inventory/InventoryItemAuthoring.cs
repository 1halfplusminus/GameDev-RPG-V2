using UnityEngine;
using Unity.Entities;

namespace RPG.Gameplay.Inventory
{

    public class InventoryItemAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public ItemDefinitionAsset ItemDefinitionAsset;

        public GameObject Item;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {

            conversionSystem.DeclareAssetDependency(gameObject, ItemDefinitionAsset);
            var blobAssetReference = conversionSystem.BlobAssetStore.GetItemDefinitionAssetBlob(ItemDefinitionAsset);
            var assetEntity = conversionSystem.GetPrimaryEntity(ItemDefinitionAsset);
            var itemPrefab = conversionSystem.GetPrimaryEntity(Item);
            dstManager.AddComponentData(entity, new ItemDefinitionReference { ItemDefinitionAssetBlob = blobAssetReference, AssetEntity = assetEntity, ItemPrefab = itemPrefab });
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
            });
        }
    }
}
