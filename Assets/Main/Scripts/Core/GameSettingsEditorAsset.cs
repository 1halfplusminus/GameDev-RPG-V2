
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RPG.Core
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "RPG/Create Game Settings", order = 0)]
    public class GameSettingsEditorAsset : ScriptableObject
    {
        public SceneAsset NewGameScene;

        public SceneAsset PlayerScene;

    }

}
#endif
