

namespace RPG.Gameplay.Inventory
{
    using System;
    // using Unity.Entities;
    using UnityEngine;

    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "RPG/Make Item Definition", order = 0)]
    public class ItemDefinitionAsset : ScriptableObject
    {
        public string ID = Guid.NewGuid().ToString();
        public string FriendlyName;
        public string Description;
        public Sprite Icon;
        public ItemDefinitionDimensions SlotDimension;
    }
    public struct ItemDefinitionSprite
    {
        public Texture Texture;
    }

    public struct BlobItemDefinition
    {
        string Description;
        string FriendlyName;


    }

    [Serializable]
    public struct ItemDefinitionDimensions
    {
        public int Height;
        public int Width;
    }
}