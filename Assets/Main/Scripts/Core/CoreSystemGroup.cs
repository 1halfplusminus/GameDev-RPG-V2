
using Unity.Entities;
namespace RPG.Core
{
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public class CoreSystemGroup : ComponentSystemGroup
    {

    }
}
