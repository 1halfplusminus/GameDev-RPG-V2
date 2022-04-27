using Unity.Entities;
using RPG.Core;
using Unity.Jobs;
using RPG.Combat;

namespace RPG.Control
{
    [GenerateAuthoringComponent]
    public struct AutoAttack : IComponentData { }

    [UpdateInGroup(typeof(ControlSystemGroup))]
    public partial class AutoAttackSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithAll<WasHitted, AutoAttack>()
            .WithNone<IsFighting, IsDeadTag>()
            .ForEach((ref Fighter fighter, in DynamicBuffer<WasHitteds> hitted) =>
             {
                 if (hitted.Length > 0)
                 {
                     fighter.Target = hitted[0].Hitter;
                     fighter.MoveTowardTarget = true;
                 }
             }).ScheduleParallel();
        }
    }
}
