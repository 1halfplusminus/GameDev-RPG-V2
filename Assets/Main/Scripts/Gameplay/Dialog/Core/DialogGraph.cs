
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;

namespace RPG.Gameplay
{
    public struct PlayDialog : IComponentData
    {
        public int CurrentIndex;
    }
    public struct Dialog : IComponentData
    {
        public BlobAssetReference<BlobDialog> Reference;

    }
    [Serializable]
    public struct NodeData
    {
        public String GUID;
        public float2 Position;
    }

    [Serializable]
    public struct DialogEdgeAssetData
    {
        public String OutputPortName;
        public String InputPortName;
        public String InputNode;
        public String OutputNode;

        public static implicit operator BlobDialogEdge(DialogEdgeAssetData dialogEdge)
        {
            var hashInputNode = new UnityEngine.Hash128();
            hashInputNode.Append(dialogEdge.InputNode);
            var hastOutputNode = new UnityEngine.Hash128();
            hastOutputNode.Append(dialogEdge.OutputNode);
            return new BlobDialogEdge { InputNode = hashInputNode, OutputNode = hastOutputNode, InputPortName = dialogEdge.InputPortName, OutputPortName = dialogEdge.OutputPortName };
        }
    }
    [Serializable]
    public struct DialogNodeAssetData
    {
        public String Text;
        public String GUID;

        public float2 Position;


        public static implicit operator BlobDialogNode(DialogNodeAssetData dialogNode)
        {
            var hash = new UnityEngine.Hash128();
            hash.Append(dialogNode.GUID);
            return new BlobDialogNode { GUID = hash, Text = dialogNode.Text };
        }
    }
    public struct BlobDialogEdge
    {

        public FixedString512 OutputPortName;
        public FixedString512 InputPortName;
        public Unity.Entities.Hash128 InputNode;
        public Unity.Entities.Hash128 OutputNode;
    }
    public struct BlobDialogNode
    {
        public FixedString512 Text;
        public Unity.Entities.Hash128 GUID;

        public BlobArray<BlobPtr<BlobDialogChoice>> Choices;

        public BlobArray<int> ChoicesIndex;
    }
    public struct BlobDialogChoice
    {
        public FixedString512 Text;
        public BlobPtr<BlobDialogNode> Next;
        public int NextIndex;
    }
    public struct BlobDialog
    {
        public BlobArray<BlobDialogNode> Nodes;
        public BlobArray<BlobDialogChoice> Choises;
        public BlobPtr<BlobDialogNode> Start;
        public int StartIndex;
    }
    public static class BlobAssetStoreExtension
    {

        public static BlobAssetReference<BlobDialog> GetDialog(this BlobAssetStore store, DialogGraph graph, Allocator allocator = Allocator.Persistent)
        {

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var dialog = ref blobBuilder.ConstructRoot<BlobDialog>();
            var blobNodes = blobBuilder.Allocate(ref dialog.Nodes, graph.nodes.Count);
            var blobChoices = blobBuilder.Allocate(ref dialog.Choises, graph.edges.Count);
            var indexMap = new Dictionary<string, int>();
            for (var i = 0; i < graph.nodes.Count; i++)
            {
                BlobDialogNode node = graph.nodes[i];
                blobNodes[i] = node;
                indexMap.Add(graph.nodes[i].GUID, i);
            }
            var choicesByNode = new Dictionary<string, List<int>>();
            for (var i = 0; i < graph.edges.Count; i++)
            {
                var edge = graph.edges[i];
                var choice = new BlobDialogChoice { Text = edge.OutputPortName };
                if (!indexMap.ContainsKey(edge.OutputNode))
                {
                    var index = indexMap[edge.InputNode];
                    ref var node = ref blobNodes[indexMap[edge.InputNode]];
                    dialog.StartIndex = index;
                    blobBuilder.SetPointer(ref dialog.Start, ref node);

                }
                var nextIndex = indexMap[edge.InputNode];
                ref var nextNode = ref blobNodes[nextIndex];
                choice.NextIndex = nextIndex;
                blobChoices[i] = choice;
                blobBuilder.SetPointer(ref blobChoices[i].Next, ref nextNode);
                if (!choicesByNode.ContainsKey(edge.OutputNode))
                {
                    choicesByNode.Add(edge.OutputNode, new List<int>());
                }
                choicesByNode[edge.OutputNode].Add(i);
            }
            foreach (KeyValuePair<string, List<int>> entry in choicesByNode)
            {
                if (indexMap.ContainsKey(entry.Key))
                {
                    ref var node = ref blobNodes[indexMap[entry.Key]];
                    var choicesArrayBuilder = blobBuilder.Allocate(ref node.Choices, entry.Value.Count);
                    var choicesIndexArrayBuilder = blobBuilder.Allocate(ref node.ChoicesIndex, entry.Value.Count);
                    for (int i = 0; i < entry.Value.Count; i++)
                    {
                        var choicesIndex = entry.Value[i];
                        blobBuilder.SetPointer(ref choicesArrayBuilder[i], ref blobChoices[choicesIndex]);
                        choicesIndexArrayBuilder[i] = choicesIndex;
                    }
                }
            }

            // blobBuilder.Construct(ref root.Nodes, graph.nodes.OfType<DialogNodeBlob>().ToArray());
            // blobBuilder.Construct(ref root.Edges, graph.edges.OfType<DialogEdgeBlob>().ToArray());
            var dialogRef = blobBuilder.CreateBlobAssetReference<BlobDialog>(allocator);
            blobBuilder.Dispose();
            return dialogRef;
        }

    }
    public class DialogGraph : ScriptableObject
    {
        public List<DialogNodeAssetData> nodes;
        public List<DialogEdgeAssetData> edges;

        public void Awake()
        {
            if (nodes == null)
            {
                nodes = new List<DialogNodeAssetData>();
            }
            if (edges == null)
            {
                edges = new List<DialogEdgeAssetData>();
            }
        }

        public List<DialogEdgeAssetData> GetEdges(string node)
        {
            return edges.Where((e) => e.OutputNode == node).ToList();
        }
    }
}