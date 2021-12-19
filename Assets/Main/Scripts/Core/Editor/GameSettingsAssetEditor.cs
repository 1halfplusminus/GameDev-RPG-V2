#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
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
            var assetPath = AssetDatabase.GetAssetPath(target);
            var asset = AssetDatabase.LoadAssetAtPath<GameSettingsAsset>(assetPath);
            if (asset == null)
            {
                asset = CreateInstance<GameSettingsAsset>();
                asset.name = "Test";
                AssetDatabase.AddObjectToAsset(asset, assetPath);

            }
            var subAssetGUID = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)));
            var root = VisualTreeAsset.Instantiate();
            if (target is GameSettingsEditorAsset gameSettingsAsset)
            {
                root.Q<PropertyField>("NewGameScene").RegisterValueChangeCallback((e) =>
                {
                    asset.NewGameScene = GetSceneGUID(gameSettingsAsset.NewGameScene);
                });
                root.Q<PropertyField>("PlayerScene").RegisterValueChangeCallback((e) =>
               {
                   asset.PlayerScene = GetSceneGUID(gameSettingsAsset.PlayerScene);
               });
            }

            return root;
        }


        private static string GetSceneGUID(SceneAsset scene)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(scene));
        }
    }

}

#endif