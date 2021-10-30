
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
                    hitEvents.Add(new HitEvent { Time = hit });
                }
                DstEntityManager.AddComponent<HittedByRaycast>(entity);
                DstEntityManager.AddComponentData(entity, new Fighter { WeaponRange = fighter.WeaponRange, AttackCooldown = fighter.AttackCooldown, AttackDuration = fighter.AttackDuration });
                DstEntityManager.AddComponent<LookAt>(entity);
                DstEntityManager.AddComponent<DeltaTime>(entity);
                /*                 DstEntityManager.AddComponentData(entity, new CharacterState { State = CharacterStateMask.Dead | CharacterStateMask.Moving }); */
            });
        }
    }

}
