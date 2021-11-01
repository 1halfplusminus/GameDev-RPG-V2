
using Unity.Entities;
using Unity.Mathematics;
using Unity.Animation;

namespace RPG.Mouvement
{
    [UpdateInGroup(typeof(MouvementSystemGroup))]
    public class PlayMouvementAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithChangeFilter<Mouvement>()
            .ForEach((ref CharacterAnimation characterAnimation, in Mouvement mouvement) =>
            {
                if (mouvement.Speed > 0.0f)
                {
                    characterAnimation.Run = 0.0f;
                    var zLinear = math.abs(mouvement.Velocity.Linear.z) / mouvement.Speed;
                    characterAnimation.Move = math.min(zLinear, 1.0f);
                    if (math.abs(mouvement.Velocity.Linear).z >= 2.5f)
                    {
                        characterAnimation.Run = zLinear * 2;
                        characterAnimation.Run = math.min(characterAnimation.Run, 1.0f);
                    }
                }

            }).ScheduleParallel();
        }
    }
}
