using RPG.Core;
using Unity.Mathematics;
using UnityEngine;


namespace RPG.Control
{
    public class ChasePlayerAuthoring : MonoBehaviour, IDrawGizmo
    {
        public float ChaseDistance;

        public float SuspiciousTime;

        [Range(0f, 360f)]
        public float AngleOfView;
        // Called by unity
        public void OnDrawGizmosSelected()
        {
            OnDrawGizmosSelected(transform);
        }
        public void OnDrawGizmosSelected(Transform transform)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, ChaseDistance);

        }
    }

    public class ConvertChasePlayerSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ChasePlayerAuthoring chasePlayer) =>
            {
                var entity = GetPrimaryEntity(chasePlayer);
                DstEntityManager.AddComponent<AIControlled>(entity);
                // Fighter should add this
                DstEntityManager.AddComponent<DeltaTime>(entity);
                DstEntityManager.AddComponentData(entity, new ChasePlayer { ChaseDistance = chasePlayer.ChaseDistance, AngleOfView = chasePlayer.AngleOfView, ChaseDistanceSq = chasePlayer.ChaseDistance * chasePlayer.ChaseDistance });
                if (chasePlayer.SuspiciousTime >= 0)
                {
                    DstEntityManager.AddComponentData(entity, new Suspicious { Time = chasePlayer.SuspiciousTime });
                }
            });
        }
    }

}