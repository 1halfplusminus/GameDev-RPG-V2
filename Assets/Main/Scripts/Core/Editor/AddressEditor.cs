

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace RPG.Core
{
    [CustomEditor(typeof(AddressableAuthoring))]
    public class AddressableAuthoringEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var target = serializedObject.targetObject;
            var addressProperty = serializedObject.FindProperty(nameof(AddressableAuthoring.Address));
            var guuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target));
            Debug.Log(guuid);
            addressProperty.stringValue = guuid;
            var root = new VisualElement();
            var addressField = new PropertyField(addressProperty);
            addressField.SetEnabled(false);
            root.Add(addressField);
            serializedObject.ApplyModifiedProperties();
            AssetReference assetReference = new AssetReference(guuid);
            var asset = assetReference.LoadAsset<GameObject>();
            if (asset.Result != null)
            {
                Debug.Log(asset.Result.name);
            }

            return root;
        }
    }
    // [CustomPropertyDrawer(typeof(Addressable))]
    // public class AddressDrawer : PropertyDrawer
    // {
    //     public override VisualElement CreatePropertyGUI(SerializedProperty property)
    //     {
    //         var target = property.serializedObject.targetObject;
    //         var container = new VisualElement();
    //         var amountField = new PropertyField(property.FindPropertyRelative("address"));

    //         return base.CreatePropertyGUI(property);
    //     }
    // }
}