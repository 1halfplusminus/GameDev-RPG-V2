using Unity.Animation;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Entities;
using static Unity.DataFlowGraph.NodeSetAPI;

namespace RPG.Animation
{

    public struct CharacterAnimationData : IAnimationData
    {
        public GraphHandle Graph;

        public NodeHandle<ConvertDeltaTimeToFloatNode> DeltaTimeNode;

        public NodeHandle<ClipPlayerNode> IdleClipPlayerNode;

        public NodeHandle<ClipPlayerNode> WalkClipPlayerNode;

        public NodeHandle<ClipPlayerNode> RunClipPlayerNode;

        public NodeHandle<MixerNode> MoveMixerNode;

        public NodeHandle<MixerNode> RunMixerNode;

        public NodeHandle<ComponentNode> EntityNode;

        public NodeHandle<ExtractCharacterAnimationParametersNode> ExtractCharacterAnimationParametersNode;
    }

    public struct CharacterAnimationSetup : IAnimationSetup
    {
        public BlobAssetReference<Clip> IDLE;

        public BlobAssetReference<Clip> Walk;

        public BlobAssetReference<Clip> Run;
    }


    public class CharacterAnimationSystemBase : AnimationSystemBase<CharacterAnimationSetup, CharacterAnimationData, ProcessDefaultAnimationGraph>
    {
        protected override CharacterAnimationData CreateGraph(Entity e, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref CharacterAnimationSetup setup)
        {
            GraphHandle graph = graphSystem.CreateGraph();
            var data = new CharacterAnimationData()
            {
                Graph = graph,
                IdleClipPlayerNode = graphSystem.CreateNode<ClipPlayerNode>(graph),
                WalkClipPlayerNode = graphSystem.CreateNode<ClipPlayerNode>(graph),
                RunClipPlayerNode = graphSystem.CreateNode<ClipPlayerNode>(graph),
                DeltaTimeNode = graphSystem.CreateNode<ConvertDeltaTimeToFloatNode>(graph),
                MoveMixerNode = graphSystem.CreateNode<MixerNode>(graph),
                RunMixerNode = graphSystem.CreateNode<MixerNode>(graph),
                EntityNode = graphSystem.CreateNode(graph, e),
                ExtractCharacterAnimationParametersNode = graphSystem.CreateNode<ExtractCharacterAnimationParametersNode>(graph),
            };
            var set = graphSystem.Set;
            set.Connect(data.EntityNode, data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
            set.Connect(data.EntityNode, data.ExtractCharacterAnimationParametersNode, ExtractCharacterAnimationParametersNode.KernelPorts.Input, NodeSet.ConnectionType.Feedback);

            SetupMixer(rig, setup, data, set, data.IdleClipPlayerNode, data.WalkClipPlayerNode, data.MoveMixerNode, ExtractCharacterAnimationParametersNode.KernelPorts.MovingOutput);


            SetupClipAnimationNode(rig, data, setup.Run, data.RunClipPlayerNode, set);
            set.SendMessage(data.RunMixerNode, MixerNode.SimulationPorts.Rig, rig);

            set.Connect(data.MoveMixerNode, MixerNode.KernelPorts.Output, data.RunMixerNode, MixerNode.KernelPorts.Input0);
            set.Connect(data.RunClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.RunMixerNode, MixerNode.KernelPorts.Input1);
            set.Connect(data.ExtractCharacterAnimationParametersNode, ExtractCharacterAnimationParametersNode.KernelPorts.RunOuput, data.RunMixerNode, MixerNode.KernelPorts.Weight);

            set.Connect(data.RunMixerNode, MixerNode.KernelPorts.Output, data.EntityNode, NodeSet.ConnectionType.Feedback);


            return data;
        }

        private static void SetupMixer(Rig rig, CharacterAnimationSetup setup, CharacterAnimationData data, NodeSet set, NodeHandle<ClipPlayerNode> clip0, NodeHandle<ClipPlayerNode> clip1, NodeHandle<MixerNode> mixer, DataOutput<ExtractCharacterAnimationParametersNode, float> extractParamaterOutput)
        {
            SetupClipAnimationNode(rig, data, setup.IDLE, clip0, set);
            SetupClipAnimationNode(rig, data, setup.Walk, clip1, set);
            set.SendMessage(mixer, MixerNode.SimulationPorts.Rig, rig);

            set.Connect(clip0, ClipPlayerNode.KernelPorts.Output, mixer, MixerNode.KernelPorts.Input0);
            set.Connect(clip1, ClipPlayerNode.KernelPorts.Output, mixer, MixerNode.KernelPorts.Input1);
            set.Connect(data.ExtractCharacterAnimationParametersNode, extractParamaterOutput, mixer, MixerNode.KernelPorts.Weight);
        }

        private static void SetupClipAnimationNode(Rig rig, CharacterAnimationData data, BlobAssetReference<Clip> clip, NodeHandle<ClipPlayerNode> node, NodeSet set)
        {
            set.SetData(node, ClipPlayerNode.KernelPorts.Speed, 1.0f);
            set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, node, ClipPlayerNode.KernelPorts.DeltaTime);
            set.SendMessage(node, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            set.SendMessage(node, ClipPlayerNode.SimulationPorts.Rig, rig);
            set.SendMessage(node, ClipPlayerNode.SimulationPorts.Clip, clip);
        }

        private static void ConnectKernetPorts(CharacterAnimationData data, NodeHandle<ConvertDeltaTimeToFloatNode> deltaTimeNode, NodeHandle<ComponentNode> entityNode, NodeSet set)
        {
            set.Connect(entityNode, deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
            set.Connect(deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, data.IdleClipPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime);
            set.Connect(data.IdleClipPlayerNode, ClipPlayerNode.KernelPorts.Output, entityNode, NodeSetAPI.ConnectionType.Feedback);
        }

        protected override void DestroyGraph(Entity e, ProcessDefaultAnimationGraph graphSystem, ref CharacterAnimationData data)
        {
            var set = graphSystem.Set;
            set.Destroy(data.IdleClipPlayerNode);
            set.Destroy(data.WalkClipPlayerNode);
            set.Destroy(data.RunClipPlayerNode);
            set.Destroy(data.DeltaTimeNode);
            set.Destroy(data.EntityNode);
            set.Destroy(data.MoveMixerNode);
            set.Destroy(data.RunMixerNode);
            set.Destroy(data.ExtractCharacterAnimationParametersNode);
        }
    }

    public class ExtractCharacterAnimationParametersNode : KernelNodeDefinition<ExtractCharacterAnimationParametersNode.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<ExtractCharacterAnimationParametersNode, CharacterAnimation> Input;
            public DataOutput<ExtractCharacterAnimationParametersNode, float> MovingOutput;

            public DataOutput<ExtractCharacterAnimationParametersNode, float> RunOuput;

        }

        public struct KernelData : IKernelData { }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, in KernelData data, ref KernelDefs ports)
            {
                var move = ctx.Resolve(ports.Input).Move;
                ctx.Resolve(ref ports.MovingOutput) = move;
                ctx.Resolve(ref ports.RunOuput) = ctx.Resolve(ports.Input).Run;
            }
        }
    }
}