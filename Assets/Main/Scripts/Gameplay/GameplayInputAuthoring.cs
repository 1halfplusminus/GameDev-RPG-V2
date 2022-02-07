using Unity.Entities;

namespace RPG.Gameplay
{
    [GenerateAuthoringComponent]
    public struct GameplayInput : IComponentData
    {
        public bool DialogInteractionPressedThisFrame;
        public bool OpenInventoryPressedThisFrame;

        public bool CloseInventoryPressedThisFrame;
    }
}