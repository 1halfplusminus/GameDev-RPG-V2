using Unity.Entities;
using RPG.Control;
using UnityEngine;

namespace RPG.Gameplay
{
    // [ExecuteAlways]
    [UpdateAfter(typeof(ControlSystemGroup))]
    public class GameplaySystemGroup : ComponentSystemGroup { }
}
