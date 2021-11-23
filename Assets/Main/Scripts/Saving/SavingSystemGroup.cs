using Unity.Entities;

namespace RPG.Saving
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class SavingSystemGroup : ComponentSystemGroup { }
}
