
using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Mouvement
{
    [UpdateInGroup(typeof(MouvementSystemGroup))]
    public partial class PlayMouvementAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithAny<IsMoving>()
            .WithChangeFilter<Mouvement>()
            .ForEach((ref CharacterAnimation characterAnimation, in Mouvement mouvement) =>
            {

                if (mouvement.Velocity.Linear.z > 0.0f)
                {
                    var zLinear = math.abs(mouvement.Velocity.Linear.z) / mouvement.Speed;

                    characterAnimation.Move = math.min(characterAnimation.Move + 0.1f, 1.0f);
                    if (zLinear >= 0.7f)
                    {
                        characterAnimation.Run = zLinear;
                        characterAnimation.Run = math.min(characterAnimation.Run + 0.01f, 1.0f);
                    }
                    else
                    {
                        characterAnimation.Run = math.max(characterAnimation.Run - 0.1f, 0.0f);
                    }
                }
                else
                {
                    characterAnimation.Run = math.max(characterAnimation.Run - 0.1f, 0.0f);
                    characterAnimation.Move = math.max(characterAnimation.Move - 0.1f, 0.0f);
                }

            }).ScheduleParallel();
            Entities
           .WithNone<IsMoving>()
           .ForEach((ref CharacterAnimation characterAnimation, in Mouvement mouvement) =>
           {
               characterAnimation.Run = math.max(characterAnimation.Run - 0.1f, 0.0f);
               characterAnimation.Move = math.max(characterAnimation.Move - 0.1f, 0.0f);
           }).ScheduleParallel();
        }
    }
}
