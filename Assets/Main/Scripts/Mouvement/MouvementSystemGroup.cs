
using Unity.Entities;

namespace RPG.Mouvement
{
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public class MouvementSystemGroup : ComponentSystemGroup
    {

    }

}
