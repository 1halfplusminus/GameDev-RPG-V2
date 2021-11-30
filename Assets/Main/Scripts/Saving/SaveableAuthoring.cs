
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEngine;

namespace RPG.Saving
{

    [Serializable]
    public struct SaveableType
    {
        [SerializeField]
        public string Id;
    }
    [ExecuteAlways]
    public class SaveableAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField]
        List<SaveableType> SaveableTypes = new List<SaveableType>();

        [SerializeField]
        public string UniqueIdentifier = "";


        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
#if UNITY_EDITOR
            var saveablesTypes = new FixedList128<ComponentType>();
            foreach (var saveableType in SaveableTypes)
            {
                var type = ComponentType.ReadOnly(Type.GetType(saveableType.Id));
                saveablesTypes.Add(type);
            }
            dstManager.AddComponentData(entity, new Saveable { types = saveablesTypes });
#endif
        }
#if UNITY_EDITOR
        private void Update()
        {
            if (Application.IsPlaying(gameObject)) { return; }
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
            {
                Debug.Log("In prefab");
                return;
            }
            var serializedObject = new SerializedObject(this);
            var property = serializedObject.FindProperty(nameof(UniqueIdentifier));
            if (property.stringValue == "")
            {
                property.stringValue = Guid.NewGuid().ToString();
                serializedObject.ApplyModifiedProperties();
            }


        }
#endif

    }
}
