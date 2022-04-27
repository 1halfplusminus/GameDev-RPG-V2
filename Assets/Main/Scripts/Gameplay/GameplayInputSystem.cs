
using RPG.Core;
using Unity.Entities;
using UnityEngine;

namespace RPG.Gameplay
{
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial class GameplayInputSystem : SystemBase
    {
        InputSystem inputSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            inputSystem = World.GetOrCreateSystem<InputSystem>();

        }
        protected override void OnUpdate()
        {
            var input = inputSystem.Input;
            var dialogInteraction = input.Gameplay.InGameInteraction.WasReleasedThisFrame();
            var openInventoryPressedThisFrame = input.Gameplay.OpenInventory.WasReleasedThisFrame();
            var closeInventoryPressedThisFrame = input.Gameplay.CloseInventory.WasReleasedThisFrame();
            Entities.ForEach((ref GameplayInput gameplayInput) =>
            {
                gameplayInput.DialogInteractionPressedThisFrame = dialogInteraction;
                gameplayInput.OpenInventoryPressedThisFrame = openInventoryPressedThisFrame;
                gameplayInput.CloseInventoryPressedThisFrame = closeInventoryPressedThisFrame;
            }).ScheduleParallel();
        }


    }
}