// using RPG.Core;
// using Unity.Animation;
// using Unity.Entities;
// using UnityEngine;
// using Unity.DataFlowGraph;
// using Unity.Jobs;
// using Unity.Burst;
// using static Unity.DataFlowGraph.NodeSetAPI;

// namespace RPG.Animation
// {

//     public struct CharacterAnimationGraphSetup : IAnimationSetup
//     {
//         public BlobAssetReference<Clip> IDLE;

//         public BlobAssetReference<Clip> Walk;

//         public BlobAssetReference<Clip> Run;

//         public BlobAssetReference<Clip> Attack;

//         public BlobAssetReference<Clip> Dead;
//     }

//     class CharacterAnimationGraphAuthoring : MonoBehaviour
//     {
//         public ClipAsset IDLE;

//         public ClipAsset Walk;

//         public ClipAsset Run;

//         public ClipAsset Attack;

//         public ClipAsset Dead;
//     }

//     public class CharacterAnimationGraphConversionSystem : GameObjectConversionSystem
//     {
//         protected override void OnUpdate()
//         {
//             Entities.ForEach((CharacterAnimationGraphAuthoring characterAnimation) =>
//             {
//                 var entity = GetPrimaryEntity(characterAnimation);
//                 var setup = new CharacterAnimationGraphSetup { };
//                 if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.IDLE, out var idleClip))
//                 {
//                     setup.IDLE = idleClip;
//                 }
//                 if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Walk, out var walkClip))
//                 {
//                     setup.Walk = walkClip;
//                 }
//                 if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Run, out var runClip))
//                 {
//                     setup.Run = runClip;
//                 }
//                 if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Attack, out var attackClip))
//                 {
//                     setup.Attack = attackClip;
//                 }
//                 if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Dead, out var deadClip))
//                 {
//                     setup.Dead = deadClip;
//                 }
//                 DstEntityManager.AddComponent<CharacterAnimation>(entity);
//                 DstEntityManager.AddComponentData(entity, setup);
//                 DstEntityManager.AddComponent<DeltaTime>(entity);
//             });
//         }

//     }
//     public struct CharacterAnimationData : IAnimationData
//     {
//         public GraphHandle Graph;

//         public NodeHandle<ConvertDeltaTimeToFloatNode> DeltaTimeNode;

//         public NodeHandle<ClipPlayerNode> IdleClipPlayerNode;

//         public NodeHandle<ClipPlayerNode> WalkClipPlayerNode;

//         public NodeHandle<ClipPlayerNode> RunClipPlayerNode;

//         public NodeHandle<ClipPlayerNode> AttackClipPlayerNode;

//         public NodeHandle<ClipPlayerNode> DeadClipPlayerNode;

//         public NodeHandle<MixerNode> MoveMixerNode;

//         public NodeHandle<MixerNode> RunMixerNode;

//         public NodeHandle<MixerNode> AttackMixerNode;

//         public NodeHandle<MixerNode> DeadMixerNode;

//         public NodeHandle<ComponentNode> EntityNode;

//         public NodeHandle<ExtractCharacterAnimationParametersNode> ExtractCharacterAnimationParametersNode;
//     }

//     [DisableAutoTypeRegistration]
//     [DisableAutoCreation]
//     public partial class  CharacterAnimationSystemBase : AnimationSystemBase<CharacterAnimationGraphSetup, CharacterAnimationData, ProcessDefaultAnimationGraph>
//     {
//         protected override CharacterAnimationData CreateGraph(Entity e, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref CharacterAnimationGraphSetup setup)
//         {
//             GraphHandle graph = graphSystem.CreateGraph();
//             var data = new CharacterAnimationData()
//             {
//                 Graph = graph,
//                 IdleClipPlayerNode = graphSystem.CreateNode<ClipPlayerNode>(graph),
//                 WalkClipPlayerNode = graphSystem.CreateNode<ClipPlayerNode>(graph),
//                 RunClipPlayerNode = graphSystem.CreateNode<ClipPlayerNode>(graph),
//                 AttackClipPlayerNode = graphSystem.CreateNode<ClipPlayerNode>(graph),
//                 DeadClipPlayerNode = graphSystem.CreateNode<ClipPlayerNode>(graph),
//                 DeltaTimeNode = graphSystem.CreateNode<ConvertDeltaTimeToFloatNode>(graph),
//                 MoveMixerNode = graphSystem.CreateNode<MixerNode>(graph),
//                 RunMixerNode = graphSystem.CreateNode<MixerNode>(graph),
//                 AttackMixerNode = graphSystem.CreateNode<MixerNode>(graph),
//                 DeadMixerNode = graphSystem.CreateNode<MixerNode>(graph),
//                 EntityNode = graphSystem.CreateNode(graph, e),
//                 ExtractCharacterAnimationParametersNode = graphSystem.CreateNode<ExtractCharacterAnimationParametersNode>(graph),
//             };
//             var set = graphSystem.Set;

//             ConnectKernetPorts(data, set);

//             SetupMoveNode(rig, setup, data, set);
//             SetupRunNode(rig, setup, data, set);
//             SetupAttackNode(rig, setup, data, set);

//             SetupDeadNode(rig, setup, data, set);

//             set.Connect(data.DeadMixerNode, MixerNode.KernelPorts.Output, data.EntityNode, ConnectionType.Feedback);

//             return data;
//         }

//         private static void SetupDeadNode(Rig rig, CharacterAnimationGraphSetup setup, CharacterAnimationData data, NodeSet set)
//         {
//             SetupClipAnimationNode(rig, data, setup.Dead, data.DeadClipPlayerNode, set, ClipConfigurationMask.NormalizedTime);
//             set.SendMessage(data.DeadMixerNode, MixerNode.SimulationPorts.Rig, rig);
//             set.Connect(data.AttackMixerNode, MixerNode.KernelPorts.Output, data.DeadMixerNode, MixerNode.KernelPorts.Input0);
//             set.Connect(data.DeadClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.DeadMixerNode, MixerNode.KernelPorts.Input1);
//             set.Connect(data.ExtractCharacterAnimationParametersNode, ExtractCharacterAnimationParametersNode.KernelPorts.DeadOutput, data.DeadMixerNode, MixerNode.KernelPorts.Weight);
//         }

//         private static void SetupMoveNode(Rig rig, CharacterAnimationGraphSetup setup, CharacterAnimationData data, NodeSet set)
//         {
//             SetupMixer(rig, setup, data, set, data.IdleClipPlayerNode, data.WalkClipPlayerNode, data.MoveMixerNode, ExtractCharacterAnimationParametersNode.KernelPorts.MovingOutput);
//         }

//         private static void SetupAttackNode(Rig rig, CharacterAnimationGraphSetup setup, CharacterAnimationData data, NodeSet set)
//         {
//             SetupClipAnimationNode(rig, data, setup.Attack, data.AttackClipPlayerNode, set);
//             set.SendMessage(data.AttackMixerNode, MixerNode.SimulationPorts.Rig, rig);
//             set.Connect(data.RunMixerNode, MixerNode.KernelPorts.Output, data.AttackMixerNode, MixerNode.KernelPorts.Input0);
//             set.Connect(data.AttackClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.AttackMixerNode, MixerNode.KernelPorts.Input1);
//             set.Connect(data.ExtractCharacterAnimationParametersNode, ExtractCharacterAnimationParametersNode.KernelPorts.AttackOutput, data.AttackMixerNode, MixerNode.KernelPorts.Weight);
//         }

//         private static void SetupRunNode(Rig rig, CharacterAnimationGraphSetup setup, CharacterAnimationData data, NodeSet set)
//         {
//             SetupClipAnimationNode(rig, data, setup.Run, data.RunClipPlayerNode, set);
//             set.SendMessage(data.RunMixerNode, MixerNode.SimulationPorts.Rig, rig);

//             set.Connect(data.MoveMixerNode, MixerNode.KernelPorts.Output, data.RunMixerNode, MixerNode.KernelPorts.Input0);
//             set.Connect(data.RunClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.RunMixerNode, MixerNode.KernelPorts.Input1);
//             set.Connect(data.ExtractCharacterAnimationParametersNode, ExtractCharacterAnimationParametersNode.KernelPorts.RunOuput, data.RunMixerNode, MixerNode.KernelPorts.Weight);

//         }

//         private static void SetupMixer(Rig rig, CharacterAnimationGraphSetup setup, CharacterAnimationData data, NodeSet set, NodeHandle<ClipPlayerNode> clip0, NodeHandle<ClipPlayerNode> clip1, NodeHandle<MixerNode> mixer, DataOutput<ExtractCharacterAnimationParametersNode, float> extractParamaterOutput)
//         {
//             SetupClipAnimationNode(rig, data, setup.IDLE, clip0, set);
//             SetupClipAnimationNode(rig, data, setup.Walk, clip1, set);
//             set.SendMessage(mixer, MixerNode.SimulationPorts.Rig, rig);

//             set.Connect(clip0, ClipPlayerNode.KernelPorts.Output, mixer, MixerNode.KernelPorts.Input0);
//             set.Connect(clip1, ClipPlayerNode.KernelPorts.Output, mixer, MixerNode.KernelPorts.Input1);
//             set.Connect(data.ExtractCharacterAnimationParametersNode, extractParamaterOutput, mixer, MixerNode.KernelPorts.Weight);
//         }

//         private static void SetupClipAnimationNode(Rig rig, CharacterAnimationData data, BlobAssetReference<Clip> clip, NodeHandle<ClipPlayerNode> node, NodeSet set, ClipConfigurationMask clipConfigurationMask = ClipConfigurationMask.LoopTime)
//         {
//             set.SetData(node, ClipPlayerNode.KernelPorts.Speed, 1.0f);
//             set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, node, ClipPlayerNode.KernelPorts.DeltaTime);
//             set.SendMessage(node, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = clipConfigurationMask });
//             set.SendMessage(node, ClipPlayerNode.SimulationPorts.Rig, rig);
//             set.SendMessage(node, ClipPlayerNode.SimulationPorts.Clip, clip);
//         }

//         private static void ConnectKernetPorts(CharacterAnimationData data, NodeSet set)
//         {
//             set.Connect(data.EntityNode, data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
//             set.Connect(data.EntityNode, data.ExtractCharacterAnimationParametersNode, ExtractCharacterAnimationParametersNode.KernelPorts.Input, ConnectionType.Feedback);
//         }

//         protected override void DestroyGraph(Entity e, ProcessDefaultAnimationGraph graphSystem, ref CharacterAnimationData data)
//         {
//             var set = graphSystem.Set;
//             if (set.IsCreated)
//             {
//                 set.Destroy(data.DeltaTimeNode);
//                 set.Destroy(data.EntityNode);

//                 set.Destroy(data.IdleClipPlayerNode);
//                 set.Destroy(data.WalkClipPlayerNode);
//                 set.Destroy(data.RunClipPlayerNode);
//                 set.Destroy(data.AttackClipPlayerNode);
//                 set.Destroy(data.DeadClipPlayerNode);

//                 set.Destroy(data.MoveMixerNode);
//                 set.Destroy(data.RunMixerNode);
//                 set.Destroy(data.AttackMixerNode);
//                 set.Destroy(data.DeadMixerNode);

//                 set.Destroy(data.ExtractCharacterAnimationParametersNode);
//             }

//         }
//     }

//     public class ExtractCharacterAnimationParametersNode : KernelNodeDefinition<ExtractCharacterAnimationParametersNode.KernelDefs>
//     {
//         public struct KernelDefs : IKernelPortDefinition
//         {
//             public DataInput<ExtractCharacterAnimationParametersNode, CharacterAnimation> Input;
//             public DataOutput<ExtractCharacterAnimationParametersNode, float> MovingOutput;
//             public DataOutput<ExtractCharacterAnimationParametersNode, float> RunOuput;
//             public DataOutput<ExtractCharacterAnimationParametersNode, float> AttackOutput;

//             public DataOutput<ExtractCharacterAnimationParametersNode, float> DeadOutput;

//         }

//         public struct KernelData : IKernelData { }

//         [BurstCompile]
//         public struct Kernel : IGraphKernel<KernelData, KernelDefs>
//         {
//             public void Execute(RenderContext ctx, in KernelData data, ref KernelDefs ports)
//             {
//                 var move = ctx.Resolve(ports.Input).Move;
//                 ctx.Resolve(ref ports.MovingOutput) = move;
//                 ctx.Resolve(ref ports.RunOuput) = ctx.Resolve(ports.Input).Run;
//                 ctx.Resolve(ref ports.AttackOutput) = ctx.Resolve(ports.Input).Attack;
//                 ctx.Resolve(ref ports.DeadOutput) = ctx.Resolve(ports.Input).Dead;
//             }
//         }
//     }
//     [DisableAutoCreation]
//     [UpdateInGroup(typeof(AnimationSystemGroup))]
//     [UpdateAfter(typeof(CharacterAnimationSystemBase))]
//     public partial class ChangeAttackAnimationSystem : SystemBase
//     {
//         private ProcessDefaultAnimationGraph animationSystem;
//         private EntityCommandBufferSystem entityCommandBufferSystem;
//         protected override void OnCreate()
//         {
//             base.OnCreate();
//             animationSystem = World.GetOrCreateSystem<ProcessDefaultAnimationGraph>();
//             entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
//         }
//         protected override void OnUpdate()
//         {

//             var cb = entityCommandBufferSystem.CreateCommandBuffer();
//             Entities
//             .WithChangeFilter<ChangeAttackAnimation>()
//             .ForEach((Entity e, in ChangeAttackAnimation attackAnimation, in CharacterAnimationData characterAnimation) => ChangeAnimation(e, attackAnimation, characterAnimation, cb))
//             .WithoutBurst()
//             .Run();
//             entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
//         }
//         [BurstCompile]
//         private void ChangeAnimation(Entity e, ChangeAttackAnimation attackAnimation, CharacterAnimationData characterAnimation, EntityCommandBuffer cb)
//         {
//             var set = animationSystem.Set;
//             UnityEngine.Debug.Log("Change attack animation");
//             if (attackAnimation.Animation.IsCreated)
//             {
//                 set.SendMessage(characterAnimation.AttackClipPlayerNode, ClipPlayerNode.SimulationPorts.Clip, attackAnimation.Animation);
//             }
//             cb.RemoveComponent<ChangeAttackAnimation>(e);
//         }
//     }
// }
