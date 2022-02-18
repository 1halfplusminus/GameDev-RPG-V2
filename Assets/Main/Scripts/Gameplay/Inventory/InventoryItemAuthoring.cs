using UnityEngine;
using Unity.Entities;

namespace RPG.Gameplay.Inventory
{

    public class InventoryItemAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public ItemDefinitionAsset ItemDefinitionAsset;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {

            conversionSystem.DeclareAssetDependency(gameObject, ItemDefinitionAsset);
            var blobAssetReference = conversionSystem.BlobAssetStore.GetItemDefinitionAssetBlob(ItemDefinitionAsset);
            var assetEntity = conversionSystem.GetPrimaryEntity(ItemDefinitionAsset);
            dstManager.AddComponentData(entity, new ItemDefinitionReference { ItemDefinitionAssetBlob = blobAssetReference, AssetEntity = assetEntity });
        }


    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    class ItemDefinitionAssetDeclare : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((InventoryItemAuthoring itemDefinitionAssetAuthoring) =>
            {
                Debug.Log($"Declare item definition {itemDefinitionAssetAuthoring.ItemDefinitionAsset.ID}");
                DeclareReferencedAsset(itemDefinitionAssetAuthoring.ItemDefinitionAsset);

            });
        }
    }
}
