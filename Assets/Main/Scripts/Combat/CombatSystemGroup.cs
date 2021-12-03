using Unity.Entities;
using RPG.Mouvement;

namespace RPG.Combat
{
    [UpdateAfter(typeof(MouvementSystemGroup))]
    public class CombatSystemGroup : ComponentSystemGroup
    {

    }
}
