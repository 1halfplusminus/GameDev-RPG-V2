using UnityEngine;
using Unity.Entities;
using RPG.Gameplay.Inventory;

namespace RPG.Gameplay.Inventory
{

    public class InventoryItemAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public ItemDefinitionAsset ItemDefinitionAsset;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {

            conversionSystem.DeclareAssetDependency(gameObject, ItemDefinitionAsset);
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
