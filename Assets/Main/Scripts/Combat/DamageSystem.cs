using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Combat
{
    //FIXME: Add a require for update
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class DamageSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithNone<NoDamage>()
            .ForEach((ref Hit hit) =>
            {
                if (HasComponent<Fighter>(hit.Hitter))
                {
                    var fighter = GetComponent<Fighter>(hit.Hitter);
                    hit.Damage = fighter.Damage;
                }
            })
            .ScheduleParallel();
        }
    }
}