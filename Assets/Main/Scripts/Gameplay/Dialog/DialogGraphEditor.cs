
#if UNITY_EDITOR
using RPG.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;



public class DialogGraphEditor : EditorWindow
{
    [MenuItem("Window/UI Toolkit/DialogGraphEditor")]
    public static void ShowExample()
    {
        DialogGraphEditor wnd = GetWindow<DialogGraphEditor>();
        wnd.titleContent = new GUIContent("DialogGraphEditor");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        root.style.flexGrow = 1;

        // Import UXML
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Main/Scripts/Gameplay/Dialog/DialogGraphEditor.uxml");
        visualTree.CloneTree(root);


    }
}

#endif