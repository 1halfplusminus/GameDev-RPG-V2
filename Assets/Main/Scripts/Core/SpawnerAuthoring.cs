#if UNITY_EDITOR
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using System.IO;


namespace RPG.Core
{
    // [ways]
    public class SpawnerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public bool HasHybridComponent;

        public bool GameObjectSpawn;

        public GameObject Prefab;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (transform.childCount > 0)
            {
                var firstChild = transform.GetChild(0)?.gameObject;
                var firstChildEntity = conversionSystem.GetPrimaryEntity(firstChild);
                if (firstChildEntity != Entity.Null)
                {
                    dstManager.AddComponent<Disabled>(firstChildEntity);
                    conversionSystem.PostUpdateCommands.DestroyEntity(firstChildEntity);
                }
            }

        }

        public void Update()
        {
            if (Application.IsPlaying(gameObject)) { return; }
            // if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null)
            // {
            //     return;
            // }
            var serializedObject = new SerializedObject(this);
            var property = serializedObject.FindProperty(nameof(Prefab));
            if (property.objectReferenceValue == null)
            {
                var dir = "Assets/Tmp";
                Directory.CreateDirectory(dir);
                var prefab = transform.GetChild(0).gameObject;
                var copy = Instantiate(prefab);
                copy.AddComponent<StopConvertToEntity>();
                copy.AddComponent<Destroy>();
                copy.SetActive(false);
                GameObject newInstanceofSpawn = PrefabUtility.SaveAsPrefabAsset(copy, $"{dir}/{prefab.GetInstanceID()}.prefab");
                property.objectReferenceValue = newInstanceofSpawn;
                serializedObject.ApplyModifiedProperties();
                DestroyImmediate(copy);
            }

        }
    }
    public class SpawnerConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((SpawnerAuthoring spawner) =>
            {
                var prefab = spawner.Prefab;
                var prefabEntity = GetPrimaryEntity(prefab);
                var entity = GetPrimaryEntity(spawner);
                DstEntityManager.AddComponentData(entity, new Spawn { Prefab = prefabEntity });
                DstEntityManager.AddComponentData(entity, new LocalToWorld { Value = spawner.transform.localToWorldMatrix });
                if (spawner.HasHybridComponent)
                {
                    DstEntityManager.AddComponent<HasHybridComponent>(entity);
                }
                if (spawner.GameObjectSpawn)
                {
                    DstEntityManager.AddComponent<GameObjectSpawner>(entity);
                    DstEntityManager.AddComponentObject(entity, spawner.gameObject);
                }
            });
        }
    }

    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class SpawnerDeclarePrefabsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((SpawnerAuthoring spawner) =>
            {

                DeclareReferencedPrefab(spawner.Prefab);
            });
        }
    }
}

#endif