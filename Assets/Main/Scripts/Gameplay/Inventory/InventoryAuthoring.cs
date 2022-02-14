

using System;
using System.Collections.Generic;
using RPG.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RPG.Gameplay.Inventory
{
    [Serializable]
    public class InventoryItemAuthoringReference : ComponentReference<InventoryItemAuthoring>
    {
        public InventoryItemAuthoringReference(string guid) : base(guid)
        {
        }
    }

    public class InventoryAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        [SerializeField]
        int2 Dimension;

        [SerializeField]
        InventoryItemAuthoringReference[] Items;

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            foreach (var item in Items)
            {
                item.ReleaseAsset();
                AsyncOperationHandle<GameObject> handle = item.LoadAssetAsync<GameObject>();
                handle.WaitForCompletion();
                referencedPrefabs.Add(handle.Result);
            }
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new Inventory { Height = Dimension.y, Width = Dimension.x });
            var itemsBuffer = dstManager.AddBuffer<InventoryItem>(entity);
            foreach (var itemHandle in Items)
            {
                var itemAuthoringGO = (GameObject)itemHandle.OperationHandle.Result;
                var itemAuthoring = itemAuthoringGO.GetComponent<InventoryItemAuthoring>();
                if (itemAuthoring != null)
                {
                    var itemEntity = conversionSystem.GetPrimaryEntity(itemAuthoring.ItemDefinitionAsset);
                    var itemDefinitionBlobAsset = conversionSystem.BlobAssetStore.GetItemDefinitionAssetBlob(itemAuthoring.ItemDefinitionAsset);
                    if (itemDefinitionBlobAsset.IsCreated)
                    {
                        itemsBuffer.Add(new InventoryItem { ItemDefinitionAsset = itemDefinitionBlobAsset, Item = itemEntity });
                    }
                }
            }
        }
    }


}