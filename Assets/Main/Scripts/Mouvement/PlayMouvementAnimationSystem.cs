
using Unity.Entities;
using Unity.Mathematics;
using RPG.Animation;

namespace RPG.Mouvement
{
    public class PlayMouvementAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithChangeFilter<Mouvement>().ForEach((ref BlendTree1DData player, in Mouvement mouvement) =>
            {
                player.paramX = math.abs(mouvement.Velocity.Linear.z);
            }).ScheduleParallel();
        }
    }
}
