
#if UNITY_EDITOR
using RPG.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;



public class DialogGraphEditor : EditorWindow
{
    [MenuItem("Graph/Dialog Editor")]
    public static void ShowWindow()
    {
        DialogGraphEditor wnd = GetWindow<DialogGraphEditor>();
        wnd.titleContent = new GUIContent("Dialog Editor");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        root.style.flexGrow = 1;

        // Import UXML
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Main/Scripts/Gameplay/Dialog/Editor/DialogGraphEditor.uxml");
        visualTree.CloneTree(root);

    }
}

#endif