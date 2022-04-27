
using Unity.Entities;
// using Unity.Animation;


namespace RPG.Core
{


    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    // [UpdateBefore(typeof(DefaultAnimationSystemGroup))]

    public class CoreSystemGroup : ComponentSystemGroup
    {

    }
}
