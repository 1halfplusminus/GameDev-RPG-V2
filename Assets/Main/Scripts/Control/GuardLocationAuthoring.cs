using UnityEngine;
using RPG.Core;
namespace RPG.Control
{
    public class GuardLocationAuthoring : MonoBehaviour
    {
        [SerializeField]
        public bool GuardOriginalLocation;

        public Vector3 GuardLocation;

        public void Start()
        {
            if (GuardOriginalLocation)
            {
                GuardLocation = transform.position;
            }
        }

    }

    public class GuardLocationConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((GuardLocationAuthoring guardLocationAuthoring) =>
            {
                var entity = GetPrimaryEntity(guardLocationAuthoring);
                DstEntityManager.AddComponentData<GuardLocation>(entity, new GuardLocation { Value = guardLocationAuthoring.GuardLocation });
                DstEntityManager.AddComponent<LookAt>(entity);
                if (guardLocationAuthoring.GuardOriginalLocation)
                {
                    DstEntityManager.AddComponent<GuardOriginalLocationTag>(entity);
                }
            });
        }
    }
}