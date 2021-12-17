using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using RPG.Core;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;

namespace RPG.UI
{
    [CustomEditor(typeof(GameUIAsset))]
    public class GameUIAssetInspector : Editor
    {
        public VisualTreeAsset VisualTreeAsset;
        public override VisualElement CreateInspectorGUI()
        {
            if (VisualTreeAsset == null) { return null; }

            var root = VisualTreeAsset.Instantiate();

            var entry = target.SetAddressableGroup(UIDeclareReferencedObjectsConversionSystem.UI_GROUP_LABEL);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings)
            {
                var group = settings.FindGroup(UIDeclareReferencedObjectsConversionSystem.UI_GROUP_LABEL);
                entry.SetLabel(UIDeclareReferencedObjectsConversionSystem.UI_ADDRESSABLE_LABEL, true, true);
                entry.SetAddress(name, true);
                var entriesModified = new List<AddressableAssetEntry> { entry };
                group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entriesModified, false, true);
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entriesModified, true, false);

            }
            return root;
        }
    }
}
