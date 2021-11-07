using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Combat
{
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class DamageSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // TODO Refractor: Get that data async

            var fighters = GetComponentDataFromEntity<Fighter>(true);
            Entities
            .WithReadOnly(fighters)
            .ForEach((ref Hit hit) =>
            {
                if (fighters.HasComponent(hit.Hitter))
                {
                    hit.Damage = fighters[hit.Hitter].WeaponDamage;
                }
            })
            .ScheduleParallel();
        }
    }
}