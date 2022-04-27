using Unity.Collections;
using Unity.Entities;

namespace RPG.Saving
{
    public struct Saveable : IComponentData
    {
        public FixedList128Bytes<ComponentType> types;
    }
}

