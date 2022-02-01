#if UNITY_EDITOR


using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
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

            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Main/Scripts/Gameplay/Dialog/Editor/DialogGraphToolbar.uxml");
            visualTree.CloneTree(this);
            var fileNameTextfield = this.Q<TextField>("Filename");
            fileNameTextfield.RegisterValueChangedCallback((e) => { fileName = $"{e.newValue}"; });
            fileName = fileNameTextfield.value;
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
                dialogGraphView.AddDialogNode();
            };
        }

        private void LoadData()
        {
            string filePath = GetFilePath();
            GetDialogGraphView().LoadData(filePath);
        }
        public static string GetCurrentAssetDirectory()
        {
            foreach (var obj in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (System.IO.Directory.Exists(path))
                    return path;
                else if (System.IO.File.Exists(path))
                    return System.IO.Path.GetDirectoryName(path);
            }

            return "Assets";
        }
        private void SaveData()
        {
            string filePath = GetFilePath();
            GetDialogGraphView().SaveData(filePath);
        }

        private string GetFilePath()
        {
            return $"{GetCurrentAssetDirectory()}/{fileName}.asset";
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
        public readonly float2 DEFAULT_SIZE = new float2(100, 150);
        public const string DIALOG_NODE_NAME = "Dialog";

        public const string DIALOG_NODE_CLASS = "dialog-node";
        private float2 mousePosition;

        private Node entryPoint;
        public new class UxmlFactory : UxmlFactory<DialogGraphView, GraphView.UxmlTraits>
        {

        }
        public DialogGraphView()
        {
            new SelectionDragger { target = this };
            new ContentDragger { target = this };
            new RectangleSelector { target = this };
            new ContentZoomer { target = this };
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Main/Scripts/Gameplay/Dialog/Editor/DialogGraphEditor.uss");

            styleSheets.Add(styleSheet);
            Insert(0, new GridBackground());
            AddElement(GenerateEntrPointNode());
        }
        private (DialogGraph value, bool loaded) CreateOrLoadAsset(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<DialogGraph>(path);
            var loaded = true;
            if (!asset)
            {
                asset = ScriptableObject.CreateInstance<DialogGraph>();
                loaded = false;
            }
            return (asset, loaded);
        }
        private void SaveAsset((DialogGraph value, bool loaded) asset, string path)
        {
            if (!asset.loaded)
            {
                ProjectWindowUtil.CreateAsset(asset.value, path);
            }
            else
            {
                // asset.serialized.ApplyModifiedProperties();
                // asset.serialized.UpdateIfRequiredOrScript();
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(path));
                AssetDatabase.SaveAssets();
            }
        }
        public void LoadData(string path)
        {
            ClearNodes();
            var (asset, loaded) = CreateOrLoadAsset(path);
            var index = new Dictionary<string, DialogGraphNode>();
            this.Query<DialogGraphNode>().ForEach((n) =>
            {
                index.Add(n.GUID, n);
            });
            asset.nodes.ForEach((n) =>
            {
                var node = AddDialogNode(n);
                index.Add(n.GUID, node);
            });

            asset.edges.ForEach((e) =>
            {
                var inputNode = index[e.InputNode];
                var outputNode = index[e.OutputNode];

                var outputPort = outputNode.outputContainer.Query<Port>().Where((p) => p.portName == e.OutputPortName).First();
                if (outputPort == null)
                {
                    outputPort = AddChoicePort(outputNode, e.OutputPortName);
                }
                var inputPort = inputNode.inputContainer.Query<Port>().Where((p) => p.portName == e.InputPortName).First();
                var edge = new Edge { input = inputPort, output = outputPort };
                AddElement(edge);
                inputPort.ConnectTo(outputPort);
                // var port = AddChoicePort(outputNode, e.OutputPortName);
                // port.ConnectTo(inputNode.Input);
            });
        }
        public void SaveData(string path)
        {
            var (asset, loaded) = CreateOrLoadAsset(path);
            AssetDatabase.StartAssetEditing();
            SaveData(asset);
            AssetDatabase.StopAssetEditing();
            SaveAsset((asset, loaded), path);

        }

        private void SaveData(DialogGraph asset)
        {
            asset.nodes.Clear();
            asset.edges.Clear();
            this.Query<DialogGraphNode>().Where((n) => !n.EntryPoint).ForEach((n) =>
            {
                asset.nodes.Add(new DialogNodeAssetData { GUID = n.GUID, Text = n.DialogueText, Position = n.GetPosition().position });
            });
            this.Query<Edge>().Where((p) => !p.isGhostEdge).ForEach((e) =>
            {
                if (e.input.node is DialogGraphNode inputNode && e.output.node is DialogGraphNode outputNode)
                {
                    asset.edges.Add(new DialogEdgeAssetData { InputNode = inputNode.GUID, OutputNode = outputNode.GUID, OutputPortName = e.output.portName, InputPortName = e.input.portName });
                }
            });
        }

        private void ClearNodes()
        {
            this.Query<Edge>().ForEach((e) =>
            {
                e.input.Disconnect(e);
                e.output.Disconnect(e);
                RemoveElement(e);
            });
            this.Query<DialogGraphNode>().Where((n) => !n.EntryPoint).ForEach((n) =>
            {
                RemoveElement(n);
            });

        }
        private Node GenerateEntrPointNode()
        {
            var node = new DialogGraphNode { title = "Start", DialogueText = "Start", EntryPoint = true, GUID = "Start" };

            var port = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Capacity.Single, typeof(float));
            port.portName = "Next";
            node.outputContainer.Add(port);

            node.RefreshExpandedState();
            node.RefreshPorts();

            node.SetPosition(new UnityEngine.Rect { x = 100, y = 200, width = DEFAULT_SIZE.x, height = DEFAULT_SIZE.y });

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
        public void AddDialogNode(string nodeName = DIALOG_NODE_NAME)
        {
            var guid = GUID.Generate();
            var node = new DialogGraphNode { title = nodeName, EntryPoint = false, GUID = guid.ToString() };
            AddDialogNode(node, Capacity.Multi);
        }

        private DialogGraphNode AddDialogNode(DialogNodeAssetData node)
        {
            var createdNode = AddDialogNode(new DialogGraphNode { DialogueText = node.Text, GUID = node.GUID }, Capacity.Multi);
            createdNode.SetPosition(new UnityEngine.Rect { x = node.Position.x, y = node.Position.y, width = DEFAULT_SIZE.x, height = DEFAULT_SIZE.y });
            return createdNode;
        }
        private DialogGraphNode AddDialogNode(DialogGraphNode node, Capacity capacity = Capacity.Single)
        {
            if (String.IsNullOrEmpty(node.title))
            {
                node.title = DIALOG_NODE_NAME;
            }
            var dialogTextField = new TextField() { multiline = true, value = node.DialogueText };
            dialogTextField.RegisterValueChangedCallback((e) =>
            {
                node.DialogueText = e.newValue;
            });
            node.mainContainer.Add(dialogTextField);
            // node.name = DIALOG_NODE_NAME;
            node.AddToClassList(DIALOG_NODE_CLASS);
            var port = node.InstantiatePort(Orientation.Horizontal, Direction.Input, capacity, typeof(float));
            port.portName = "Input";
            node.inputContainer.Add(port);

            var button = new Button(clickEvent: () =>
            {
                AddChoicePort(node);
            });
            button.text = "New Choice";
            node.titleContainer.Add(button);

            node.RefreshExpandedState();
            node.RefreshPorts();

            AddElement(node);
            return node;
        }

        private Port AddChoicePort(DialogGraphNode node, string name = "")
        {
            var generatedPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Capacity.Single, typeof(float));
            var outputPortCount = node.outputContainer.Query("connector").ToList().Count;

            generatedPort.portName = name;
            if (String.IsNullOrEmpty(generatedPort.portName))
            {
                generatedPort.portName = $"Choice {outputPortCount}";
            }
            var portNameTextField = new TextField { value = generatedPort.portName };
            portNameTextField.RegisterValueChangedCallback((e) =>
            {
                generatedPort.portName = e.newValue;
            });
            var label = generatedPort.contentContainer.Q<Label>();
            label.style.display = DisplayStyle.None;
            generatedPort.contentContainer.Add(portNameTextField);
            node.outputContainer.Add(generatedPort);

            node.RefreshExpandedState();
            node.RefreshPorts();
            return generatedPort;
        }
    }
}

#endif