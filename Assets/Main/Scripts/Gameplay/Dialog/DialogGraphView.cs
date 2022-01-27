#if UNITY_EDITOR


using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.Port;

namespace RPG.Gameplay
{
    public class DialogGraphViewToolbar : Toolbar
    {
        private DialogGraphView dialogGraphView;
        private string fileName;
        public new class UxmlFactory : UxmlFactory<DialogGraphViewToolbar, GraphView.UxmlTraits>
        {

        }
        public DialogGraphViewToolbar()
        {

            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Main/Scripts/Gameplay/Dialog/DialogGraphToolbar.uxml");
            visualTree.CloneTree(this);
            var fileNameTextfield = this.Q<TextField>("Filename");
            fileNameTextfield.RegisterValueChangedCallback((e) => { fileName = e.newValue; });
            var saveButton = this.Q<Button>("SaveButton");
            saveButton.clicked += () =>
            {
                SaveData();
            };
            var loadButton = this.Q<Button>("LoadButton");
            loadButton.clicked += () =>
            {
                LoadData();
            };
            var createDialogNodeButton = this.Q<ToolbarButton>("CreateDialogNode");
            createDialogNodeButton.clicked += () =>
            {
                var dialogGraphView = GetDialogGraphView();
                dialogGraphView.CreateDialogNode("Dialog");
            };
        }

        private void LoadData()
        {
            throw new NotImplementedException();
        }

        private void SaveData()
        {
            throw new NotImplementedException();
        }

        private DialogGraphView GetDialogGraphView()
        {
            if (dialogGraphView == null)
            {
                dialogGraphView = parent.Q<DialogGraphView>();
            }
            return dialogGraphView;
        }
    }
    public class DialogGraphView : GraphView
    {
        public new class UxmlFactory : UxmlFactory<DialogGraphView, GraphView.UxmlTraits>
        {

        }
        public DialogGraphView()
        {
            new SelectionDragger { target = this };
            new ContentDragger { target = this };
            new RectangleSelector { target = this };
            new ContentZoomer { target = this };
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Main/Scripts/Gameplay/Dialog/DialogGraphEditor.uss");
            Debug.Log("On Enable");

            styleSheets.Add(styleSheet);
            Insert(0, new GridBackground());
            AddElement(GenerateEntrPointNode());
        }

        private Node GenerateEntrPointNode()
        {
            var node = new DialogGraphNode { title = "Start", DialogueText = "Start", EntryPoint = true };

            var port = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Capacity.Single, typeof(float));
            port.portName = "Next";
            node.outputContainer.Add(port);

            node.RefreshExpandedState();
            node.RefreshPorts();

            node.SetPosition(new UnityEngine.Rect { x = 100, y = 200, width = 100, height = 150 });
            return node;
        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach((port) =>
             {
                 if (startPort != port && startPort.node != port.node)
                 {
                     compatiblePorts.Add(port);
                 }
             });
            return compatiblePorts;
        }
        // public override void List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter){

        // }
        public void CreateDialogNode(string nodeName)
        {
            var node = new DialogGraphNode { title = nodeName, DialogueText = nodeName, EntryPoint = false, GUID = new GUID().ToString() };

            var port = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Capacity.Single, typeof(float));
            port.portName = "Input";
            node.inputContainer.Add(port);

            var button = new Button(clickEvent: () =>
            {
                AddChoicePort(node);
            });
            button.text = "New Choise";
            node.titleContainer.Add(button);

            node.RefreshExpandedState();
            node.RefreshPorts();

            AddElement(node);
        }

        private void AddChoicePort(DialogGraphNode node)
        {
            var generatedPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Capacity.Single, typeof(float));
            var outputPortCount = node.outputContainer.Query("connector").ToList().Count;
            generatedPort.portName = $"Choice {outputPortCount}";

            node.outputContainer.Add(generatedPort);

            node.RefreshExpandedState();
            node.RefreshPorts();
        }
    }
}

#endif