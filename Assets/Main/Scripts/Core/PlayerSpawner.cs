
using UnityEngine;

namespace RPG.Core
{
    [ExecuteInEditMode]
    public class PlayerSpawner : MonoBehaviour
    {
        public GameObject Prefab;

        public bool HasHybridComponent;
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

}
