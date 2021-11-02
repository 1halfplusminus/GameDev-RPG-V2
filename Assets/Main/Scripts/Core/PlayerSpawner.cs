
using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using Unity.Transforms;

namespace RPG.Core
{
    [ExecuteInEditMode]
    public class PlayerSpawner : MonoBehaviour
    {
        public GameObject Prefab;

        public bool HasHybridComponent;


        List<IComponentData> components = new List<IComponentData>();

        void OnDrawGizmos()
        {

            foreach (var item in Prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                item.BakeMesh(mesh);
                Gizmos.DrawWireMesh(mesh, -1, transform.position - item.transform.position, transform.rotation * item.transform.rotation, transform.localScale);
            }
            foreach (var item in Prefab.GetComponentsInChildren<MeshFilter>())
            {
                Gizmos.DrawWireMesh(item.mesh, -1, item.transform.position, item.transform.rotation, item.transform.localScale);
            }

        }

        void OnDrawGizmosSelected()
        {
            foreach (var item in Prefab.GetComponentsInChildren<IDrawGizmo>())
            {
                item.OnDrawGizmosSelected(transform);
            }
        }

    }
    public class SpawnConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PlayerSpawner spawner) =>
            {
                var prefabEntity = GetPrimaryEntity(spawner.Prefab);
                var entity = GetPrimaryEntity(spawner);
                DstEntityManager.AddComponentData(entity, new Spawn { Prefab = prefabEntity });
                DstEntityManager.AddComponentData(entity, new LocalToWorld { Value = spawner.transform.localToWorldMatrix });
                if (spawner.HasHybridComponent)
                {
                    DstEntityManager.AddComponent<HasHybridComponent>(entity);
                }
            });
        }
    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class SpawnDeclarePrefabsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PlayerSpawner spawner) =>
            {
                DeclareReferencedPrefab(spawner.Prefab);

            });
        }
    }
}
