using RPG.Core;
using Unity.Entities;

namespace RPG.Saving
{

    [UpdateAfter(typeof(CoreSystemGroup))]
    public class SavingSystemGroup : ComponentSystemGroup { }
}
