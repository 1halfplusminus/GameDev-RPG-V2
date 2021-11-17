#if UNITY_EDITOR
using Unity.Entities;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
public class SaveEditorWindow : EditorWindow
{
    World CurrentWorld;

    [MenuItem("Tools/Test Save")]
    static void Save()
    {
        // Opens the window, otherwise focuses it if it’s already open.
        var window = GetWindow<SaveEditorWindow>();

        // Adds a title to the window.
        window.titleContent = new GUIContent("Save Debug");

        // Sets a minimum size to the window.
        window.minSize = new Vector2(250, 50);

        Debug.Log(World.DefaultGameObjectInjectionWorld);

    }


    private void OnEnable()
    {

        // Reference to the root of the window.
        var root = rootVisualElement;

        // Creates our button and sets its Text property.
        var saveButton = new Button() { text = "Save" };

        // Gives it some style.
        saveButton.style.width = 160;
        saveButton.style.height = 30;

        saveButton.clicked += () =>
        {
            var saveSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SaveSystem>();
            saveSystem.Save();
        };
        // Adds it to the root.
        root.Add(saveButton);


        // Creates our button and sets its Text property.
        var loadButton = new Button() { text = "Load" };

        // Gives it some style.
        loadButton.style.width = 160;
        loadButton.style.height = 30;
        loadButton.clicked += () =>
        {
            var saveSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SaveSystem>();
            saveSystem.Load();
        };
        // Adds it to the root.
        root.Add(loadButton);
    }
}
#endif