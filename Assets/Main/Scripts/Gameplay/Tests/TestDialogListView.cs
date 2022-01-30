

using RPG.Gameplay;
using RPG.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace RPG.Test
{
    public class TestDialogListView : VisualElement
    {

        public new class UxmlFactory : UxmlFactory<TestDialogListView, TestDialogListView.UxmlTraits>
        {

        }
        private DialogController controller;
        public TestDialogListView()
        {
            var dialogAsset = Resources.Load<DialogGraph>("Dialog");
            var handle = Addressables.LoadAssetAsync<VisualTreeAsset>("In Game Dialog");
            handle.Completed += (onCompleted) =>
           {
               foreach (var styleSheet in onCompleted.Result.stylesheets)
               {
                   styleSheets.Add(styleSheet);
               }
               onCompleted.Result.CloneTree(this);
               var store = new BlobAssetStore();
               var dialog = store.GetDialog(dialogAsset);
               var controller = new DialogController();
               controller.Init(this);
               controller.ShowNode(dialog, dialog.Value.StartIndex);
           };
            handle.WaitForCompletion();
        }
    }
}