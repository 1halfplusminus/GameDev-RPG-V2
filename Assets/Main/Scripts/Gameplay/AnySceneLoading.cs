using Unity.Entities;

namespace RPG.Gameplay
{

    [GenerateAuthoringComponent]
    public struct AnySceneLoading : IComponentData
    {
        public bool Value;
    }
}
