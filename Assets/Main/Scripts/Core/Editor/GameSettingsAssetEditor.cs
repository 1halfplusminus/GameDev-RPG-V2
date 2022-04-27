#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


namespace RPG.Core
{
    [CustomEditor(typeof(GameSettingsEditorAsset))]
    public class GameSettingsAssetEditor : Editor
    {
        public VisualTreeAsset VisualTreeAsset;
        public override VisualElement CreateInspectorGUI()
        {
            if (VisualTreeAsset == null) { return null; }
            var root = VisualTreeAsset.Instantiate();
            if (target is GameSettingsEditorAsset gameSettingsAsset)
            {
                root.Q<PropertyField>("NewGameScene").RegisterValueChangeCallback((e) =>
                {
                    GameSettingsAsset asset = LoadSettingsAsset();
                    asset.NewGameScene = GetSceneGUID(gameSettingsAsset.NewGameScene);
                });
                root.Q<PropertyField>("PlayerScene").RegisterValueChangeCallback((e) =>
                {
                    GameSettingsAsset asset = LoadSettingsAsset();
                    asset.PlayerScene = GetSceneGUID(gameSettingsAsset.PlayerScene);
                });
            }

            return root;
        }

        private GameSettingsAsset LoadSettingsAsset()
        {
            var assetPath = AssetDatabase.GetAssetPath(target);
            var asset = AssetDatabase.LoadAssetAtPath<GameSettingsAsset>(assetPath);
            if (asset == null)
            {
                asset = CreateInstance<GameSettingsAsset>();
                asset.name = "Test";
                AssetDatabase.AddObjectToAsset(asset, assetPath);
                AssetDatabase.SaveAssetIfDirty(target);
            }

            return asset;
        }

        private static string GetSceneGUID(SceneAsset scene)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(scene));
        }
    }

}

#endif