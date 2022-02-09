using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AddressableAssets;
using Unity.Collections;
using Unity.Mathematics;

namespace RPG.Gameplay.Inventory
{

    public class TestItemGrid : ItemGrid
    {
        public new class UxmlFactory : UxmlFactory<TestItemGrid, TestItemGrid.UxmlTraits>
        {

        }
        public TestItemGrid()
        {

            AddToClassList("inventory-grid");
            name = "Grid";
            var handle = Addressables.LoadAssetsAsync<ItemDefinitionAsset>("Test Item", (r) => { });
            handle.WaitForCompletion();
            var results = handle.Result;
            var inventory = new Inventory { Width = 8, Height = 6 };
            InitInventory(inventory);
            var items = new NativeArray<InventoryItem>(results.Count, Allocator.Temp);
            var i = 0;
            foreach (var item in results)
            {
                items[i] = new InventoryItem { };
                AddItem(new ItemSlotDescription { Texture = item.Icon.texture, GUID = item.ID, Dimension = new int2(item.SlotDimension.Width, item.SlotDimension.Height) });
                i++;
            }
            items.Dispose();
        }
    }
}