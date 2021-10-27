
using Unity.Entities;
using Unity.Mathematics;
using RPG.Animation;

namespace RPG.Mouvement
{
    public class PlayMouvementAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithChangeFilter<Mouvement>().ForEach((ref CharacterAnimation characterAnimation, in Mouvement mouvement) =>
            {
                if (mouvement.Velocity.Linear.Equals(float3.zero))
                {
                    characterAnimation.Move = math.abs(math.normalizesafe(mouvement.Velocity.Linear).z);
                }
                else
                {
                    characterAnimation.Move = 1f;
                }

                /*   player.paramX = math.abs(mouvement.Velocity.Linear.z); */
            }).ScheduleParallel();
        }
    }
}
