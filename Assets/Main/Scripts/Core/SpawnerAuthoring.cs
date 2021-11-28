#if UNITY_EDITOR
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using System.IO;

namespace RPG.Core
{
    [ExecuteAlways]
    public class SpawnerAuthoring : MonoBehaviour
    {
        public bool HasHybridComponent;

        public bool GameObjectSpawn;

        public GameObject Prefab;

        public void Start()
        {
            if (Prefab == null)
            {
                Debug.Log("Convert");
                var dir = "Assets/Tmp";
                Directory.CreateDirectory(dir);
                var prefab = transform.GetChild(0).gameObject;
                GameObject newInstanceofSpawn = PrefabUtility.SaveAsPrefabAsset(prefab, $"{dir}/{prefab.GetInstanceID()}.prefab");
                Prefab = newInstanceofSpawn;
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