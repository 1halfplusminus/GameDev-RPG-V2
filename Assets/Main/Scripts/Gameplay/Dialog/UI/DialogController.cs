
using System;
using System.Diagnostics;
using RPG.Gameplay;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;


namespace RPG.UI
{

    public class InteractWithUIDialogController : IComponentData
    {
        VisualElement container;
        public void Init(VisualElement root)
        {
            container = root;

        }
        public void SetPosition(Camera camera, Vector3 position)
        {

            Vector2 newPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                   container.panel, position, camera);
            newPosition.x = newPosition.x - container.layout.width / 2f;
            container.transform.position = newPosition;
            container.style.opacity = 100f;
        }

        public void Hide()
        {
            container.style.visibility = Visibility.Hidden;
        }
    }
    public class DialogController : IComponentData
    {
        public Action onClose;
        ListView listView;
        Label dialogTextLabel;
        Button closeButton;
        public BlobAssetReference<BlobDialog> Dialog;
        public int CurrentIndex;

        public void Init(VisualElement root)
        {
            listView = root.Q<ListView>("Choices");
            listView.makeItem = () => new Label();
            dialogTextLabel = root.Q<Label>("DialogText");
            closeButton = root.Q<Button>("Close");

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
                if (Dialog.Value.Choises.Length >= selection)
                {
                    ShowNode(Dialog, Dialog.Value.Choises[selection].NextIndex);
                }
            };

            closeButton.clicked += onClose;
        }



        public void ShowNode(in BlobAssetReference<BlobDialog> dialog, int nodeIndex)
        {
            Dialog = dialog;
            dialogTextLabel.text = dialog.Value.Nodes[nodeIndex].Text.ToString();
            listView.itemsSource = dialog.Value.Nodes[nodeIndex].ChoicesIndex.ToArray();
            listView.RefreshItems();
            if (listView.itemsSource.Count == 0)
            {
                listView.style.display = DisplayStyle.None;
                closeButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                closeButton.style.display = DisplayStyle.None;
            }
        }

    }
}