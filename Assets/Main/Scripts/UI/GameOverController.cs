using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using static RPG.UI.UIExtensions;

namespace RPG.UI
{
    public class GameOverUIController : IUIController, IComponentData
    {
        Button exitButton;
        Button tryAgainButton;

        Button mainMenuButton;

        public Action OnTryAgain;
        public Action OnMainMenu;
        public void Init(VisualElement root)
        {
            tryAgainButton = root.Q<Button>("TryAgain");
            exitButton = root.Q<Button>("Exit");
            mainMenuButton = root.Q<Button>("MainMenu");

            mainMenuButton.clicked += OnMainMenu;
            exitButton.clicked += () =>
            {
                exitButton.SetEnabled(false);
            };
            exitButton.clicked += QuitGame;

            tryAgainButton.clicked += () =>
            {
                tryAgainButton.SetEnabled(false);
            };
            tryAgainButton.clicked += OnTryAgain;
        }
    }
}