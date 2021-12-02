#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace RPG.Core
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Game/GameSettings", order = 0)]
    public class GameSettingsAsset : ScriptableObject
    {
        [SerializeField]
        public SceneAsset NewGameScene;
    }


}

#endif