
#if UNITY_EDITOR
namespace RPG.Stats
{
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ProgressionAsset))]
    public class ProgressionAssetEditor : Editor
    {
        public VisualTreeAsset VisualTreeAsset;
        public override VisualElement CreateInspectorGUI()
        {
            if (VisualTreeAsset == null) { return null; }
            var root = VisualTreeAsset.Instantiate();
            var assetPath = AssetDatabase.GetAssetPath(target);
            var assetGUID = AssetDatabase.GUIDFromAssetPath(assetPath);
            serializedObject.FindProperty(nameof(ProgressionAsset.GUID)).stringValue = assetGUID.ToString();
            return root;
        }
    }
}

#endif