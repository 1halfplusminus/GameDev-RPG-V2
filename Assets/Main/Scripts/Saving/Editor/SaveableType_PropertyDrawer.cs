#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using RPG.Saving;
using System.Collections.Generic;
using Unity.Transforms;

[CustomPropertyDrawer(typeof(SaveableType))]
public class SaveableType_PropertyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {

        // Create a new VisualElement to be the root the property UI
        var container = new VisualElement();

        //DropdownField

        var choices = new List<string>();
        var datas = new List<string>();
        choices.Add(typeof(Translation).FullName);
        var dropdownField = new DropdownField(choices, 0, (v) => { return v; })
        {
            bindingPath = property.FindPropertyRelative("Id").propertyPath,
            userData = datas
        };

        // Create drawer UI using C#
        var popup = new UnityEngine.UIElements.PopupWindow
        {
            text = "Component"
        };
        popup.Add(dropdownField);
        container.Add(popup);

        // Return the finished UI
        return container;
    }
}

#endif