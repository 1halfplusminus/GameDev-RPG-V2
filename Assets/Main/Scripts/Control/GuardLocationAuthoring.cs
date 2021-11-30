using UnityEngine;
using RPG.Core;
using RPG.Mouvement;

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
                DstEntityManager.AddComponent<LookAt>(entity);
                if (guardLocationAuthoring.GuardOriginalLocation)
                {
                    DstEntityManager.AddComponentData(entity, new GuardLocation { Value = guardLocationAuthoring.gameObject.transform.position });
                    DstEntityManager.AddComponent<GuardOriginalLocationTag>(entity);
                }
                else
                {
                    DstEntityManager.AddComponentData(entity, new GuardLocation { Value = guardLocationAuthoring.GuardLocation });
                }
            });
        }
    }
}