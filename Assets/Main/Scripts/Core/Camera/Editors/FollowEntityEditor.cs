#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Collections;
using RPG.Hybrid;
using RPG.Core;


[CustomEditor(typeof(FollowEntityAuthoring))]
public class FollowEntityEditor : Editor
{
    public VisualTreeAsset asset;
    public override VisualElement CreateInspectorGUI()
    {
        var followEntityAuthoring = target as FollowEntityAuthoring;
        var world = World.DefaultGameObjectInjectionWorld;
        var em = world.EntityManager;
        var queries = em.CreateEntityQuery(typeof(Spawn), typeof(TagFilter));
        if (asset && queries.CalculateEntityCount() > 0)
        {
            var queryResults = queries.ToEntityArray(Allocator.Temp);
            var root = asset.CloneTree();
            var dropdownField = root.Q<DropdownField>();
            dropdownField.bindingPath = "Tag";
            if (dropdownField.choices == null)
            {
                dropdownField.userData = new List<Entity>();
                dropdownField.choices = new List<string>();
            }
            var userData = dropdownField.userData as List<Entity>;
            for (int i = 0; i < queryResults.Length; i++)
            {
                var entity = queryResults[i];
                userData.Add(entity);
                var debugName = em.GetComponentData<DebugName>(entity);
                dropdownField.choices.Add(debugName.Name.ToString());
            }
            queryResults.Dispose();
            dropdownField.RegisterValueChangedCallback((changeEvent) =>
            {
                var selection = userData[dropdownField.index];
            });
            return root;
        }
        return null;

    }

}
#endif