using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AddressableAssets;
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
            foreach (var item in results)
            {
                AddItem(new ItemSlotDescription { Texture = item.Icon.texture, GUID = item.ID });
            }
        }
    }
}