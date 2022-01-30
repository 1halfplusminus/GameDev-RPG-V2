
using System.Diagnostics;
using RPG.Gameplay;
using Unity.Entities;
using UnityEngine.UIElements;


namespace RPG.UI
{

    public class DialogController : IComponentData
    {
        ListView listView;
        Label dialogTextLabel;
        public BlobAssetReference<BlobDialog> Dialog;
        public int CurrentIndex;

        public void Init(VisualElement root)
        {
            listView = root.Q<ListView>("Choices");
            listView.makeItem = () => new Label();
            dialogTextLabel = root.Q<Label>("DialogText");

            listView.bindItem = (e, i) =>
            {
                var choiceIndex = (int)listView.itemsSource[i];
                if (e is Label label)
                {
                    label.text = Dialog.Value.Choises[choiceIndex].Text.ToString();
                }
            };

            listView.onSelectionChange += (e) =>
            {
                var selection = (int)listView.itemsSource[listView.selectedIndex];
                ShowNode(Dialog, Dialog.Value.Choises[selection].NextIndex);
            };
        }



        public void ShowNode(in BlobAssetReference<BlobDialog> dialog, int nodeIndex)
        {
            Dialog = dialog;
            dialogTextLabel.text = dialog.Value.Nodes[nodeIndex].Text.ToString();
            listView.itemsSource = dialog.Value.Nodes[nodeIndex].ChoicesIndex.ToArray();
            listView.RefreshItems();
            if (listView.itemsSource.Count == 0)
            {
                listView.style.visibility = Visibility.Hidden;
            }
        }

    }
}