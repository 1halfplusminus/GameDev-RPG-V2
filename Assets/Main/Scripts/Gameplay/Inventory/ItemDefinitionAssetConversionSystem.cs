

using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace RPG.Gameplay.Inventory
{
    public struct ItemDefinitionReference : IComponentData
    {
        public BlobAssetReference<ItemDefinitionAssetBlob> ItemDefinitionAssetBlob;
        public Entity AssetEntity;

        public Entity ItemPrefab;
    }
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
        public BlobString GUID;
        public int2 Dimension;
    }
    [Serializable]
    public struct InventoryItem : IBufferElementData, IEquatable<InventoryItem>, IComparable<InventoryItem>
    {
        public int Index;

        public BlobAssetReference<ItemDefinitionAssetBlob> ItemDefinitionAsset;

        public Entity ItemDefinition;

        public Entity ItemPrefab;
        public bool IsFull;

        public bool IsEmpty { get { return !IsFull; } set { IsFull = !value; } }
        public static InventoryItem Empty => new InventoryItem { IsFull = false, ItemDefinition = Entity.Null };

        public bool HaveItem { get { return ItemDefinitionAsset.IsCreated; } }

        public int CompareTo(InventoryItem other)
        {
            return other.Index - Index;
        }

        public bool Equals(InventoryItem other)
        {
            return other.Index == Index;
        }
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
            blobBuilder.AllocateString(ref itemDefinitionAssetBlob.GUID, itemDefinitionAsset.ID);
            itemDefinitionAssetBlob.Dimension = new int2 { x = itemDefinitionAsset.SlotDimension.Width, y = itemDefinitionAsset.SlotDimension.Height };
            UnityEngine.Hash128 GUID = new UnityEngine.Hash128();
            GUID.Append(itemDefinitionAsset.ID);
            var blobAssetReferenceItemDefinition = blobBuilder.CreateBlobAssetReference<ItemDefinitionAssetBlob>(Allocator.Persistent);
            blobBuilder.Dispose();
            return blobAssetReferenceItemDefinition;
        }


    }
}