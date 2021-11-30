using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Entities;
using UnityEngine;
using RPG.Core;
namespace RPG.Animation
{

#if UNITY_EDITOR

    using Unity.Animation.Hybrid;
    [DisableAutoCreation]
    public class ClipPlayerConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ClipPlayer cp) =>
            {
                if (cp.Clip == null)
                {
                    return;
                }
                var entity = GetPrimaryEntity(cp);
                DeclareAssetDependency(cp.gameObject, cp.Clip);
                DstEntityManager.AddComponentData(entity, new PlayClip() { Clip = BlobAssetStore.GetClip(cp.Clip) });

                DstEntityManager.AddComponent<DeltaTime>(entity);
            });
        }
    }

#endif

    public class ClipPlayer : MonoBehaviour
    {

        public AnimationClip Clip;
    }

    public struct PlayClip : IAnimationSetup
    {
        public BlobAssetReference<Clip> Clip;
    }

    public struct PlayClipStateComponent : IAnimationData
    {
        public GraphHandle Graph;
        public NodeHandle<ClipPlayerNode> ClipPlayerNode;

    }

    [DisableAutoCreation]
    [UpdateBefore(typeof(DefaultAnimationSystemGroup))]
    public class PlayClipSystemBase : AnimationSystemBase<PlayClip, PlayClipStateComponent, ProcessDefaultAnimationGraph>
    {


        protected override PlayClipStateComponent CreateGraph(Entity e, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref PlayClip setup)
        {
            GraphHandle graph = graphSystem.CreateGraph();
            var data = new PlayClipStateComponent()
            {
                Graph = graph,
                ClipPlayerNode = graphSystem.CreateNode<ClipPlayerNode>(graph)
            };

            var deltaTimeNode = graphSystem.CreateNode<ConvertDeltaTimeToFloatNode>(graph);
            var entityNode = graphSystem.CreateNode(graph, e);

            var set = graphSystem.Set;

            // Connect kernel ports
            set.Connect(entityNode, deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
            set.Connect(deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, data.ClipPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime);
            set.Connect(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Output, entityNode, NodeSetAPI.ConnectionType.Feedback);

            // Send messages to set parameters on the ClipPlayerNode

            set.SetData(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Speed, 1.0f);
            set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Rig, rig);
            set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Clip, setup.Clip);

            return data;
        }

        protected override void DestroyGraph(Entity e, ProcessDefaultAnimationGraph graphSystem, ref PlayClipStateComponent data)
        {

        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            Entities
           .ForEach((Entity e, ref PlayClip playClip, ref PlayClipStateComponent state) =>
           {
               animationSystem.Set.SendMessage(state.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Clip, playClip.Clip);
           });
        }
    }
}
