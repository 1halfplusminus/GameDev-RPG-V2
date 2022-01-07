#if UNITY_EDITOR

namespace RPG.Animation
{
    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    using UnityEngine.UIElements;

    public class ConvertAnimationEditorWindow : EditorWindow
    {

        public VisualTreeAsset visualTreeAsset;

        [MenuItem("Animation/Convert")]
        private static void Convert()
        {
            var selection = Selection.objects.FirstOrDefault();

            if (selection is AnimationClip ac)
            {

                var assetPath = AssetDatabase.GetAssetPath(ac);
                var clipAsset = AssetDatabase.LoadAssetAtPath<ClipAsset>(assetPath);
                if (clipAsset == null)
                {
                    Debug.Log("Create Clip Asset");
                    clipAsset = CreateInstance<ClipAsset>();
                    AssetDatabase.AddObjectToAsset(clipAsset, assetPath);
                }
                var serializedObject = new SerializedObject(clipAsset);
                clipAsset.SetClip(ac);
                clipAsset.name = ac.name;
                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

    }

}

#endif