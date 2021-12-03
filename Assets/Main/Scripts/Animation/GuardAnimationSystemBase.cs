using Unity.Animation;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Entities;
using static Unity.DataFlowGraph.NodeSetAPI;

namespace RPG.Animation
{

    public struct GuardAnimationData : IAnimationData
    {
        public GraphHandle Graph;

        public NodeHandle<ClipPlayerNode> LookingAroundClipPlayerNode;

        public NodeHandle<MixerNode> LookingAroundMixer;


        public NodeHandle<ExtractGuardAnimationParametersNode> ExtractGuardAnimationParametersNode;

    }

    public struct GuardAnimationSetup : IAnimationSetup
    {
        public BlobAssetReference<Clip> LookingAround;
    }


    [UpdateAfter(typeof(CharacterAnimationSystemBase))]
    public class GuardAnimationnSystemBase : AnimationSystemBase<GuardAnimationSetup, GuardAnimationData, ProcessDefaultAnimationGraph>
    {
        protected override GuardAnimationData CreateGraph(Entity e, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref GuardAnimationSetup setup)
        {
            if (EntityManager.HasComponent<CharacterAnimationData>(e))
            {
                var characterAnimation = EntityManager.GetComponentData<CharacterAnimationData>(e);
                GraphHandle graph = characterAnimation.Graph;
                var data = new GuardAnimationData()
                {
                    Graph = graph,
                    LookingAroundClipPlayerNode = graphSystem.CreateNode<ClipPlayerNode>(graph),
                    LookingAroundMixer = graphSystem.CreateNode<MixerNode>(graph),
                    ExtractGuardAnimationParametersNode = graphSystem.CreateNode<ExtractGuardAnimationParametersNode>(graph)
                };
                var set = graphSystem.Set;

                set.Connect(characterAnimation.EntityNode, data.ExtractGuardAnimationParametersNode, ExtractGuardAnimationParametersNode.KernelPorts.Input, ConnectionType.Feedback);
                set.Disconnect(characterAnimation.IdleClipPlayerNode, ClipPlayerNode.KernelPorts.Output, characterAnimation.MoveMixerNode, MixerNode.KernelPorts.Input0);

                SetupLookingAroundClip(rig, setup, characterAnimation, data, set);

                set.SendMessage(data.LookingAroundMixer, MixerNode.SimulationPorts.Rig, rig);
                set.Connect(characterAnimation.IdleClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.LookingAroundMixer, MixerNode.KernelPorts.Input0);
                set.Connect(data.LookingAroundClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.LookingAroundMixer, MixerNode.KernelPorts.Input1);
                set.Connect(data.ExtractGuardAnimationParametersNode, ExtractGuardAnimationParametersNode.KernelPorts.LookingAround, data.LookingAroundMixer, MixerNode.KernelPorts.Weight);


                set.Connect(data.LookingAroundMixer, MixerNode.KernelPorts.Output, characterAnimation.MoveMixerNode, MixerNode.KernelPorts.Input0);

                return data;
            }
            return default;

        }

        private static void SetupLookingAroundClip(Rig rig, GuardAnimationSetup setup, CharacterAnimationData characterAnimation, GuardAnimationData data, NodeSet set)
        {
            set.SetData(data.LookingAroundClipPlayerNode, ClipPlayerNode.KernelPorts.Speed, 1.0f);
            set.Connect(characterAnimation.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, data.LookingAroundClipPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime);
            set.SendMessage(data.LookingAroundClipPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            set.SendMessage(data.LookingAroundClipPlayerNode, ClipPlayerNode.SimulationPorts.Rig, rig);
            set.SendMessage(data.LookingAroundClipPlayerNode, ClipPlayerNode.SimulationPorts.Clip, setup.LookingAround);
        }

        protected override void DestroyGraph(Entity e, ProcessDefaultAnimationGraph graphSystem, ref GuardAnimationData data)
        {
            var set = graphSystem.Set;
            set.Destroy(data.LookingAroundClipPlayerNode);
            set.Destroy(data.LookingAroundMixer);
            set.Destroy(data.ExtractGuardAnimationParametersNode);
        }
    }
    public class ExtractGuardAnimationParametersNode : KernelNodeDefinition<ExtractGuardAnimationParametersNode.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<ExtractGuardAnimationParametersNode, GuardAnimation> Input;
            public DataOutput<ExtractGuardAnimationParametersNode, float> LookingAround;

        }

        public struct KernelData : IKernelData { }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, in KernelData data, ref KernelDefs ports)
            {
                ctx.Resolve(ref ports.LookingAround) = ctx.Resolve(ports.Input).NervouslyLookingAround;
            }
        }
    }
}