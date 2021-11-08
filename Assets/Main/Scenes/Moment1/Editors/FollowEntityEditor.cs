using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Collections;

using RPG.Hybrid;
using UnityEditor.UIElements;

[CustomEditor(typeof(FollowEntityAuthoring))]
public class FollowEntityEditor : Editor
{
    public VisualTreeAsset asset;
    public override VisualElement CreateInspectorGUI()
    {
        var followEntityAuthoring = serializedObject.targetObject as FollowEntityAuthoring;
        var world = World.DefaultGameObjectInjectionWorld;
        var em = world.EntityManager;
        var queries = em.CreateEntityQuery(typeof(RPG.Core.Spawn));
        if (asset && queries.CalculateEntityCount() > 0)
        {
            var queryResults = queries.ToEntityArray(Allocator.Temp);
            var root = asset.CloneTree();
            var dropdownField = root.Q<DropdownField>();
            dropdownField.bindingPath = "Entity";
            var entities = new List<Entity>();
            if (dropdownField.choices == null)
            {
                dropdownField.userData = new List<Entity>();
                dropdownField.choices = new List<string>();
            }
            var userData = dropdownField.userData as List<Entity>;
            for (int i = 0; i < queryResults.Length; i++)
            {
                var entity = queryResults[i];
                entities.Add(entity);
                userData.Add(entity);
                var entityName = em.GetName(entity);
                var debugName = em.GetComponentData<DebugName>(entity);
                dropdownField.choices.Add(debugName.Name.ToString() + " " + entity.Version);
                if (entity.Index == followEntityAuthoring.Index)
                {
                    dropdownField.index = i;
                }
            }
            queryResults.Dispose();

            dropdownField.RegisterValueChangedCallback((changeEvent) =>
            {
                var selection = userData[dropdownField.index];
                if (serializedObject.targetObject is FollowEntityAuthoring followEntity)
                {
                    Debug.Log("Following " + changeEvent.newValue);
                    followEntity.Index = entities[dropdownField.index].Index;
                    followEntity.Version = entities[dropdownField.index].Version;

                    EditorUtility.SetDirty(serializedObject.targetObject);
                    serializedObject.ApplyModifiedProperties();
                }
            });
            return root;
        }
        return null;

    }
}
