using Unity.Entities;
using RPG.Combat;

namespace RPG.Control
{
    [UpdateAfter(typeof(CombatSystemGroup))]
    public class ControlSystemGroup : ComponentSystemGroup
    {

    }

}
