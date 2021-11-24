using RPG.Core;
using RPG.Gameplay;
using Unity.Entities;

namespace RPG.Saving
{

    [UpdateAfter(typeof(CoreSystemGroup))]
    public class SavingSystemGroup : ComponentSystemGroup { }
}
