using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using static RPG.Core.AddressableExtensions;
namespace RPG.Combat
{
#if UNITY_EDITOR
    [CustomEditor(typeof(WeaponAsset))]
    public class WeaponInspector : Editor
    {
        public const string GROUP = "Weapons";
        public VisualTreeAsset m_InspectorXML;
        public override VisualElement CreateInspectorGUI()
        {

            // Create a new VisualElement to be the root of our inspector UI
            var myInspector = new VisualElement();
            if (m_InspectorXML != null)
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                var entry = serializedObject.targetObject.SetAddressableGroup(GROUP);
                serializedObject.FindProperty(nameof(WeaponAsset.GUID)).stringValue = entry.address;
                serializedObject.ApplyModifiedProperties();
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
                // Return the finished inspector UI
            }

            return myInspector;
        }
        private void OnAnimationChange(SerializedPropertyChangeEvent evt)
        {

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
                serializedObject.ApplyModifiedProperties();
            }

        }
    }
#endif


}