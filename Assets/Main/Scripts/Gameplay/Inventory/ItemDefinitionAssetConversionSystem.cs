

using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace RPG.Gameplay.Inventory
{
    public struct ItemTexture : ISharedComponentData, IEquatable<ItemTexture>
    {
        public Texture2D Texture;

        public bool Equals(ItemTexture other)
        {
            return other.Texture == Texture;
        }
        public override int GetHashCode()
        {

            return Texture.GetHashCode();
        }

    }
    public struct ItemDefinitionAssetBlob
    {
        public BlobString Description;
        public BlobString FriendlyName;
        public Unity.Entities.Hash128 GUID;
        public int2 Dimension;
    }
    public struct InventoryItem : IBufferElementData
    {
        public int2 Coordinate;

        public BlobAssetReference<ItemDefinitionAssetBlob> ItemDefinitionAsset;

        public Entity Item;

    }
    public struct InventorySlot : IBufferElementData
    {
        public Entity Item;
    }
    public static class ItemDefinitionAssetBlobAssetStoreExtension
    {
        public static BlobAssetReference<ItemDefinitionAssetBlob> GetItemDefinitionAssetBlob(this BlobAssetStore blobAssetStore, ItemDefinitionAsset itemDefinitionAsset)
        {
            var hash = new UnityEngine.Hash128();
            hash.Append(itemDefinitionAsset.ID);
            blobAssetStore.TryGet<ItemDefinitionAssetBlob>(hash, out var blobAssetReference);
            if (!blobAssetReference.IsCreated)
            {
                blobAssetReference = ItemDefinitionAssetConversionSystem.Convert(itemDefinitionAsset);
            }
            return blobAssetReference;
        }
    }
    public class ItemDefinitionAssetConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ItemDefinitionAsset itemDefinitionAsset) =>
            {
                var entity = GetPrimaryEntity(itemDefinitionAsset);
                var blobAssetReferenceItemDefinition = BlobAssetStore.GetItemDefinitionAssetBlob(itemDefinitionAsset);
                DstEntityManager.AddSharedComponentData(entity, new ItemTexture { Texture = itemDefinitionAsset.Icon.texture });
            });
        }

        public static BlobAssetReference<ItemDefinitionAssetBlob> Convert(ItemDefinitionAsset itemDefinitionAsset)
        {
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var itemDefinitionAssetBlob = ref blobBuilder.ConstructRoot<ItemDefinitionAssetBlob>();

            blobBuilder.AllocateString(ref itemDefinitionAssetBlob.Description, itemDefinitionAsset.Description);
            blobBuilder.AllocateString(ref itemDefinitionAssetBlob.FriendlyName, itemDefinitionAsset.FriendlyName);
            itemDefinitionAssetBlob.Dimension = new int2 { x = itemDefinitionAsset.SlotDimension.Width, y = itemDefinitionAsset.SlotDimension.Height };
            UnityEngine.Hash128 GUID = new UnityEngine.Hash128();
            GUID.Append(itemDefinitionAsset.ID);
            itemDefinitionAssetBlob.GUID = GUID;
            var blobAssetReferenceItemDefinition = blobBuilder.CreateBlobAssetReference<ItemDefinitionAssetBlob>(Allocator.Persistent);
            blobBuilder.Dispose();
            return blobAssetReferenceItemDefinition;
        }


    }
}