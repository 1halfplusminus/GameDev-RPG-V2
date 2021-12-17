namespace RPG.UI
{
    using UnityEngine;
    using UnityEngine.UIElements;
    public enum GameUIType
    {
        MainScreen, LoadingUI
    }

    [CreateAssetMenu(fileName = "GameUIAsset", menuName = "RPG/GameUI", order = 0)]
    public class GameUIAsset : ScriptableObject
    {
        public GameObject Prefab;
        public VisualTreeAsset VisualTreeAsset;
        public GameUIType GameUIType;
    }
}