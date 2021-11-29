#if UNITY_EDITOR
using RPG.Saving;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(SaveableAuthoring))]
public class SaveableAuthoring_Inspector : Editor
{
    public VisualTreeAsset m_InspectorXML;

    public override VisualElement CreateInspectorGUI()
    {
        // Create a new VisualElement to be the root of our inspector UI
        VisualElement myInspector = new VisualElement();

        // Load from default reference
        m_InspectorXML.CloneTree(myInspector);

        // Return the finished inspector UI
        return myInspector;
    }
}

#endif