
using Unity.Entities;
using Unity.Animation;
using Unity.Physics;
using Unity.Physics.Systems;

namespace RPG.Core
{


    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateBefore(typeof(DefaultAnimationSystemGroup))]

    public class CoreSystemGroup : ComponentSystemGroup
    {

    }
}
