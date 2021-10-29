using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Combat
{
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class DamageSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .ForEach((ref Hit hit) =>
            {
                // Put all damage at one
                hit.Damage = 1.0f;
            })
            .ScheduleParallel();
        }
    }
}