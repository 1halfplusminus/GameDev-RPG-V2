using UnityEngine;
using Unity.Entities;
using RPG.Core;

namespace RPG.Control
{

    public class PatrolPathAuthoring : MonoBehaviour
    {
        const float waypointGizmoRadius = 0.3f;
        private void OnDrawGizmos()
        {

            for (int i = 0; i < transform.childCount; i++)
            {
                DrawSphere(i);
                DrawLine(i);
                DrawSphere(GetNextIndex(i));
            }
        }

        private int GetNextIndex(int i)
        {
            if (i + 1 == transform.childCount)
            {
                return 0;
            }
            return i + 1;
        }
        private void DrawLine(int i)
        {
            Gizmos.color = Color.grey;

            Gizmos.DrawLine(GetWaypoint(i).position, GetWaypoint(GetNextIndex(i)).position);
        }
        private void DrawSphere(int i)
        {
            SetSphereColor(i);
            Gizmos.DrawSphere(GetWaypoint(i).position, waypointGizmoRadius);
        }

        private void SetSphereColor(int i)
        {
            if (i == 0)
            {
                Gizmos.color = Color.blue;
            }
            else if (i + 1 == transform.childCount)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.grey;
            }
        }

        public Transform GetWaypoint(int i)
        {
            return transform.GetChild(i);
        }
    }

    public class PatrolPathsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PatrolPathAuthoring patrolPath) =>
            {
                var entity = GetPrimaryEntity(patrolPath);
                var buffer = DstEntityManager.AddBuffer<PatrolWaypoint>(entity);
                for (int i = 0; i < patrolPath.transform.childCount; i++)
                {

                    buffer.Add(new PatrolWaypoint { Position = patrolPath.GetWaypoint(i).position });
                }
            });
        }
    }
}