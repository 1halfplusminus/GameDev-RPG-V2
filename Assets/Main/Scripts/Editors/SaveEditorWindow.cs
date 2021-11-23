#if UNITY_EDITOR
using Unity.Entities;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Scenes;
using RPG.Saving;

public class SaveEditorWindow : EditorWindow
{
    [SerializeField] public VisualTreeAsset visualTreeAsset;

    public World CurrentWorld;

    [MenuItem("Tools/Test Save")]
    static void Save()
    {
        // Opens the window, otherwise focuses it if it’s already open.
        var window = GetWindow<SaveEditorWindow>();

        // Adds a title to the window.
        window.titleContent = new GUIContent("Save Debug");

        // Sets a minimum size to the window.
        window.minSize = new Vector2(250, 50);

        window.CurrentWorld = World.DefaultGameObjectInjectionWorld;

    }


    private void OnEnable()
    {

        // Reference to the root of the window.
        var root = visualTreeAsset.Instantiate();

        // Creates our button and sets its Text property.
        var saveButton = root.Q<Button>("Save");
        saveButton.clicked += () =>
        {
            var saveSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SaveSystem>();
            saveSystem.Save();
        };

        // Creates our button and sets its Text property.
        var loadButton = root.Q<Button>("Load");

        loadButton.clicked += () =>
        {
            var saveSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SaveSystem>();
            saveSystem.Load();
        };

        var scenes = root.Q<DropdownField>("Scene");
        var userData = new List<Unity.Entities.Hash128>();
        scenes.choices = new List<string>();
        scenes.userData = userData;
        var scenesQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(SubScene), typeof(SceneSectionData));
        using var sceneEntities = scenesQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < scenesQuery.CalculateEntityCount(); i++)
        {
            var sceneEntity = sceneEntities[i];
            var subScene = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentObject<SubScene>(sceneEntity);
            userData.Add(subScene.SceneGUID);
            scenes.choices.Add(subScene.SceneName);
        }

        var saveSceneButton = root.Q<Button>("SaveScene");

        saveSceneButton.clicked += () =>
        {
            var saveSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SaveSystem>();
            var sceneSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SceneSystem>();
            var sceneGUID = userData[scenes.index];
            var sceneEntity = sceneSystem.GetSceneEntity(sceneGUID);

            var query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(Identifier), typeof(SceneTag));
            query.AddSharedComponentFilter(new SceneTag() { SceneEntity = sceneEntity });
            saveSystem.Save(query);
            Debug.Log($"Saving Scene For {scenes.choices[scenes.index]} : {sceneEntity.Index} : GUID {sceneGUID}");
        };

        var loadSceneButton = root.Q<Button>("LoadScene");

        loadSceneButton.clicked += () =>
        {
            var saveSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SaveSystem>();
            var sceneSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SceneSystem>();
            var sceneGUID = userData[scenes.index];
            var sceneEntity = sceneSystem.GetSceneEntity(sceneGUID);

            var query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(Identifier), typeof(SceneTag));
            query.AddSharedComponentFilter(new SceneTag() { SceneEntity = sceneEntity });
            saveSystem.Load(query);
            Debug.Log($"Loading Scene For {scenes.choices[scenes.index]} : {sceneEntity.Index}");
        };
        rootVisualElement.Add(root);
    }
}
#endif