using UnityEditor;
using UnityEngine.UIElements;
using RPG.Core;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using UnityEditor.UIElements;

namespace RPG.UI
{
    [CustomEditor(typeof(GameUIAuthoring))]
    public class GameUIAuthoringInspector : Editor
    {
        public VisualTreeAsset VisualTreeAsset;
        public void OnUpdate()
        {
            if (target is GameUIAuthoring go)
            {
                var uiDocument = go.GetComponent<UIDocument>();
                serializedObject.FindProperty(nameof(go.VisualTreeAsset)).objectReferenceValue = uiDocument.visualTreeAsset;
                serializedObject.ApplyModifiedProperties();
            }
        }
        public override VisualElement CreateInspectorGUI()
        {
            if (VisualTreeAsset == null) { return null; }

            var root = VisualTreeAsset.Instantiate();
            root.Q<PropertyField>("VisualTreeAsset").SetEnabled(false);
            if (target is GameUIAuthoring go)
            {
                var uiDocument = go.GetComponent<UIDocument>();
                serializedObject.FindProperty(nameof(go.VisualTreeAsset)).objectReferenceValue = uiDocument.visualTreeAsset;
                serializedObject.ApplyModifiedProperties();
                if (uiDocument.visualTreeAsset != null)
                {
                    var entry = target.SetAddressableGroup(UIDeclareReferencedObjectsConversionSystem.UI_GROUP_LABEL);
                    var settings = AddressableAssetSettingsDefaultObject.Settings;
                    if (settings && entry != null)
                    {
                        var group = settings.FindGroup(UIDeclareReferencedObjectsConversionSystem.UI_GROUP_LABEL);
                        entry.SetLabel(UIDeclareReferencedObjectsConversionSystem.UI_ADDRESSABLE_LABEL, true, true);
                        entry.SetAddress(name, true);
                        var entriesModified = new List<AddressableAssetEntry> { entry };
                        group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entriesModified, false, true);
                        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entriesModified, true, false);

                    }
                }

            }

            return root;
        }
    }
}
