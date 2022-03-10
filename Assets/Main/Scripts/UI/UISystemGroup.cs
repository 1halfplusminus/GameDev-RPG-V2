using RPG.Gameplay;
using Unity.Entities;
using UnityEngine;

namespace RPG.UI
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GameplaySystemGroup))]
    public class UISystemGroup : ComponentSystemGroup { }
}