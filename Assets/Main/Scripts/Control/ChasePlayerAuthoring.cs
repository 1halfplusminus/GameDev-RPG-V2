
using RPG.Core;
using RPG.Mouvement;
using Unity.Entities;
using UnityEngine;


namespace RPG.Control
{
    public class ChasePlayerAuthoring : MonoBehaviour
    {
        public float ChaseDistance;
    }

    public class ConvertChasePlayerSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ChasePlayerAuthoring chasePlayer) =>
            {
                var entity = GetPrimaryEntity(chasePlayer);
                DstEntityManager.AddComponentData<MoveTo>(entity, new MoveTo(chasePlayer.transform.position) { Stopped = true });
                DstEntityManager.AddComponent<AIControlled>(entity);
                // Fighter should add this
                DstEntityManager.AddComponent<DeltaTime>(entity);
                DstEntityManager.AddComponentData<ChasePlayer>(entity, new ChasePlayer { ChaseDistance = chasePlayer.ChaseDistance });
            });
        }
    }

}