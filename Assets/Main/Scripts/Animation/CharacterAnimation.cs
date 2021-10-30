
using Unity.Entities;

[GenerateAuthoringComponent]
public struct CharacterAnimation : IComponentData
{
    public float Move;

    public float Attack;

    public float AttackCooldown;

    public float Run;

    public float Dead;
}