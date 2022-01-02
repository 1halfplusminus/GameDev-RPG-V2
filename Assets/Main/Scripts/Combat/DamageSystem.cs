
using RPG.Stats;
using Unity.Entities;
using UnityEngine;

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
                if (HasComponent<CalculedStat>(hit.Hitter))
                {
                    var calculedStat = GetComponent<CalculedStat>(hit.Hitter);
                    var damage = calculedStat.GetStat(Stats.Stats.Damage);
                    // Debug.Log($"Hit for {(int)damage}");
                    hit.Damage = damage;
                }
            })
            .ScheduleParallel();
        }
    }
}