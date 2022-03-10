using Unity.Entities;
using RPG.Control;
using UnityEngine;

namespace RPG.Gameplay
{
    // [ExecuteAlways]
    [UpdateAfter(typeof(ControlSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class GameplaySystemGroup : ComponentSystemGroup { }
}
