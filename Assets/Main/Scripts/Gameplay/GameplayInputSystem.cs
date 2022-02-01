
using RPG.Core;
using Unity.Entities;
using UnityEngine;

namespace RPG.Gameplay
{
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public class GameplayInputSystem : SystemBase
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
            Entities.ForEach((ref GameplayInput gameplayInput) =>
            {
                Debug.Log($"E Released this frame {dialogInteraction}");
                gameplayInput.DialogInteractionPressedThisFrame = dialogInteraction;
            }).ScheduleParallel();
        }


    }
}