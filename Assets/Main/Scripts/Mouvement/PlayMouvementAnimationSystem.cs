
using Unity.Entities;
using Unity.Mathematics;


namespace RPG.Mouvement
{
    public class PlayMouvementAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithChangeFilter<Mouvement>().ForEach((ref CharacterAnimation characterAnimation, in Mouvement mouvement) =>
            {
                if (mouvement.Speed > 0.0f)
                {
                    characterAnimation.Run = 0.0f;
                    var zLinear = mouvement.Velocity.Linear.z / mouvement.Speed;
                    characterAnimation.Move = zLinear;
                    if (math.abs(mouvement.Velocity.Linear).z >= 2.5f)
                    {
                        characterAnimation.Run = zLinear * 2;
                    }
                }

                /*   player.paramX = math.abs(mouvement.Velocity.Linear.z); */
            }).ScheduleParallel();
        }
    }
}
