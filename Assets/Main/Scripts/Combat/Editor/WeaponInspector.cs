#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.AddressableAssets;
using static RPG.Core.AddressableExtensions;
using RPG.Animation;


namespace RPG.Combat
{
    [CustomEditor(typeof(WeaponAsset))]
    public class WeaponInspector : Editor
    {
        public const string GROUP = "Weapons";
        public VisualTreeAsset m_InspectorXML;
        private VisualElement root;

        public override VisualElement CreateInspectorGUI()
        {

            // Create a new VisualElement to be the root of our inspector UI
            var myInspector = new VisualElement();

            if (m_InspectorXML != null)
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                var entry = serializedObject.targetObject.SetAddressableGroup(GROUP);
                if (entry != null)
                {
                    serializedObject.FindProperty(nameof(WeaponAsset.GUID)).stringValue = entry.address;
                    serializedObject.ApplyModifiedProperties();
                }
                // Load from default reference
                m_InspectorXML.CloneTree(myInspector);
                var guid = myInspector.Q<PropertyField>("GUID");
                guid.SetEnabled(false);
                myInspector.Q<PropertyField>("HitEvents").SetEnabled(false);
                myInspector.Q<PropertyField>("AttackDuration").SetEnabled(false);
                var animationSelector = myInspector.Q<PropertyField>("Animation");
                var updateHitEvents = myInspector.Q<Button>("UpdateEvent");
                animationSelector.RegisterValueChangeCallback(OnAnimationChange);
                updateHitEvents.clicked += UpdateAnimationData;
                myInspector.Q<Button>("ConvertEvent").clicked += ConvertClip;
                root = myInspector;
            }

            return myInspector;
        }
        private void ConvertClip()
        {
            RemoveOldClip();
            UpdateAnimationData();
            // root.Q<Button>("ConvertEvent").SetEnabled(false);
            // var animationClip = GetAnimationClip();
            // var clipProperty = GetClipProperty();
            // var assetPath = AssetDatabase.GetAssetPath(animationClip);
            // var clipAsset = AssetDatabase.LoadAssetAtPath<ClipAsset>(assetPath);
            // // while (clipAsset != null)
            // // {
            // //     Debug.Log("Remove object from asset");
            // //     AssetDatabase.RemoveObjectFromAsset(clipAsset);
            // //     AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(clipAsset));
            // //     clipAsset = AssetDatabase.LoadAssetAtPath<ClipAsset>(assetPath);
            // // }
            // if (clipAsset == null)
            // {
            //     Debug.Log("Create Clip Asset");
            //     clipAsset = CreateInstance<ClipAsset>();
            //     AssetDatabase.AddObjectToAsset(clipAsset, assetPath);
            // }
            // clipAsset.name = animationClip.name;
            // clipAsset.SetClip(animationClip);
            // clipProperty.objectReferenceValue = clipAsset;
            // serializedObject.ApplyModifiedProperties();
            // AssetDatabase.SaveAssetIfDirty(clipAsset);
            // AssetDatabase.SaveAssetIfDirty(animationClip);
            // AssetDatabase.SaveAssetIfDirty(target);
            // // root.Q<Button>("ConvertEvent").SetEnabled(true);
        }

        private void RemoveOldClip()
        {
            var assetPath = AssetDatabase.GetAssetPath(target);
            var clipAsset = AssetDatabase.LoadAssetAtPath<ClipAsset>(assetPath);
            if (clipAsset != null)
            {
                Debug.Log("Remove old clip asset");
                AssetDatabase.RemoveObjectFromAsset(clipAsset);
            }
            AssetDatabase.SaveAssetIfDirty(target);
        }

        private void OnAnimationChange(SerializedPropertyChangeEvent evt)
        {
            RemoveOldClip();
            UpdateAnimationData();
        }

        private void UpdateAnimationData()
        {

            var animationProperty = serializedObject.FindProperty(nameof(WeaponAsset.Animation));
            if (animationProperty.objectReferenceValue is AnimationClip clip)
            {
                serializedObject.FindProperty(nameof(WeaponAsset.AttackDuration)).floatValue = clip.averageDuration;
                var hitEventsProperty = serializedObject.FindProperty(nameof(WeaponAsset.HitEvents));
                hitEventsProperty.ClearArray();
                hitEventsProperty.arraySize = clip.events.Length;
                for (int i = 0; i < clip.events.Length; i++)
                {
                    var animationEvent = clip.events[i];
                    hitEventsProperty.GetArrayElementAtIndex(i).floatValue = animationEvent.time;
                }
                var assetPath = AssetDatabase.GetAssetPath(clip);
                var clipAsset = AssetDatabase.LoadAssetAtPath<ClipAsset>(assetPath);
                var clipProperty = GetClipProperty();

                if (clipAsset == null)
                {
                    Debug.LogWarning("Unable to load clip asset for animation clip");
                    // animationProperty.objectReferenceValue = null;
                    // animationProperty.serializedObject.ApplyModifiedProperties();
                    return;
                }
                clipProperty.objectReferenceValue = clipAsset;
                clipProperty.serializedObject.ApplyModifiedProperties();
                // serializedObject.ApplyModifiedProperties();
            }

        }

        private AnimationClip GetAnimationClip()
        {
            SerializedProperty animationProperty = GetAnimationClipProperty();
            if (animationProperty.objectReferenceValue is AnimationClip clip)
            {
                return clip;
            }
            return null;
        }

        private SerializedProperty GetAnimationClipProperty()
        {
            return serializedObject.FindProperty(nameof(WeaponAsset.Animation));
        }
        private SerializedProperty GetClipProperty()
        {
            return serializedObject.FindProperty(nameof(WeaponAsset.Clip));
        }
    }

}
#endif