using Unity.Entities;

namespace RPG.Core
{
    [GenerateAuthoringComponent]
    public struct InScene : IComponentData
    {
        public Unity.Entities.Hash128 SceneGUID;
    }
}

