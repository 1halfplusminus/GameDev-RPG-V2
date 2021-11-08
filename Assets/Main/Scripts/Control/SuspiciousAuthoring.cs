
using Unity.Entities;
using UnityEngine;

namespace RPG.Control
{

    public class SuspiciousAuthoring : MonoBehaviour
    {

        public float SuspiciousTime;

    }

    public class SuspiciousConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((SuspiciousAuthoring suspiciousAuthoring) =>
            {
                var entity = GetPrimaryEntity(suspiciousAuthoring);
                DstEntityManager.AddComponentData(entity, new Suspicious(suspiciousAuthoring.SuspiciousTime));
            });
        }
    }
}