
using Unity.Entities;
using RPG.Core;
using UnityEngine;

namespace RPG.Combat
{
    public class FighterConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((FighterAuthoring fighter) =>
            {
                var entity = GetPrimaryEntity(fighter);
                var hitEvents = DstEntityManager.AddBuffer<HitEvent>(entity);
                foreach (var hit in fighter.HitEvents)
                {
                    Debug.Log("add hit event");
                    hitEvents.Add(new HitEvent { Time = hit });
                }
                DstEntityManager.AddComponent<HittedByRaycast>(entity);
                DstEntityManager.AddComponentData<Fighter>(entity, new Fighter { WeaponRange = fighter.WeaponRange, AttackCooldown = fighter.AttackCooldown, AttackDuration = fighter.AttackDuration });
                DstEntityManager.AddComponent<LookAt>(entity);
                DstEntityManager.AddComponent<DeltaTime>(entity);
            });
        }
    }

}
