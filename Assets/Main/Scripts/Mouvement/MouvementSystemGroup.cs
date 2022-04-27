
using Unity.Entities;
using RPG.Core;
using Unity.Transforms;
namespace RPG.Mouvement
{
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CoreSystemGroup))]
    public class MouvementSystemGroup : ComponentSystemGroup
    {

    }

}
