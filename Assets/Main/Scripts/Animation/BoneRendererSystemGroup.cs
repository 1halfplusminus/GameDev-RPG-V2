// using Unity.Entities;
// using Unity.Animation;


// namespace RPG.Animation
// {
//     [UpdateAfter(typeof(LateAnimationSystemGroup))]
//     [DisableAutoCreation]
//     public class BoneRendererSystemGroup : ComponentSystemGroup
//     {

//     }

//     [UpdateInGroup(typeof(BoneRendererSystemGroup))]
//     [DisableAutoCreation]
//     public class BoneRendererMatrixSystem : ComputeBoneRenderingMatricesBase
//     {

//     }

//     [UpdateInGroup(typeof(BoneRendererSystemGroup))]
//     [UpdateAfter(typeof(BoneRendererMatrixSystem))]
//     [DisableAutoCreation]
//     public class BoneRendererRenderingSystem : RenderBonesBase
//     {

//     }
// }
