
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

    public struct SharedIdentifier : ISharedComponentData
    {
        public Unity.Entities.Hash128 Id;
    }
    [Serializable]
    public struct SaveableType
    {
        [SerializeField]
        public string Id;
    }
    [ExecuteAlways]
    public class SaveableAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        Entity entity;

        [SerializeField]
        List<SaveableType> SaveableTypes = new List<SaveableType>();

        [SerializeField]
        public string UniqueIdentifier = "";

        private void Start()
        {
            Debug.Log($"Start {gameObject.name}");
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            entity = em.CreateEntity();
            if (!string.IsNullOrEmpty(UniqueIdentifier) && IsUnique(UniqueIdentifier))
            {
                AddIdentifierToEntity(UniqueIdentifier);
            }

        }

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
            if (property.stringValue == "" || !IsUnique(property.stringValue))
            {
                property.stringValue = Guid.NewGuid().ToString();
                serializedObject.ApplyModifiedProperties();
                AddIdentifierToEntity(property.stringValue);
            }

        }
        private void AddIdentifierToEntity(string id)
        {
            UnityEngine.Hash128 hash = GetHashFromId(id);
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            em.AddSharedComponentData(entity, new SharedIdentifier { Id = hash });

        }

        private static UnityEngine.Hash128 GetHashFromId(string id)
        {
            var hash = new UnityEngine.Hash128();
            hash.Append(id);
            return hash;
        }

        private bool IsUnique(string id)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = em.CreateEntityQuery(typeof(SharedIdentifier));
            var filter = new SharedIdentifier { Id = GetHashFromId(id) };
            query.SetSharedComponentFilter(filter);
            var count = query.CalculateEntityCount();
            if (count == 0)
            {
                return true;
            }
            if (count > 1)
            {
                return false;
            }
            var foundEntity = query.GetSingletonEntity();
            if (entity == foundEntity)
            {
                return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                em.DestroyEntity(entity);
            }

        }
#endif

    }
}
