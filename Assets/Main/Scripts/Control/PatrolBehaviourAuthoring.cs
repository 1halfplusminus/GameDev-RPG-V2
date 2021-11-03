using UnityEngine;
using Unity.Entities;
using RPG.Core;

namespace RPG.Control
{
    public class PatrolBehaviourAuthoring : MonoBehaviour
    {
        public GameObject Path;

        public float PatrolSpeed;

        public float DwellingTime;

    }


    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class PatrolPathsDeclarePrefabsSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PatrolBehaviourAuthoring patrolingPathAuthoring) =>
            {
                DeclareReferencedPrefab(patrolingPathAuthoring.Path);
            });

        }
    }
    [UpdateAfter(typeof(PatrolPathsConversionSystem))]
    public class PatrollingPathConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PatrolBehaviourAuthoring patrolingPathAuthoring) =>
            {
                var entity = GetPrimaryEntity(patrolingPathAuthoring);
                if (DstEntityManager.HasComponent<Spawn>(entity))
                {
                    var spawn = DstEntityManager.GetComponentData<Spawn>(entity);
                    entity = spawn.Prefab;
                }
                var pathEntity = GetPrimaryEntity(patrolingPathAuthoring.Path);
                DstEntityManager.AddComponentData(entity, new PatrollingPath { Entity = pathEntity });
                DstEntityManager.AddComponentData(entity, new Patrolling(patrolingPathAuthoring.PatrolSpeed) { DwellingTime = patrolingPathAuthoring.DwellingTime });
            });
        }
    }
}