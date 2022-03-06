using RPG.Gameplay;
using Unity.Entities;
using UnityEngine;

namespace RPG.UI
{
    [ExecuteAlways]
    [UpdateAfter(typeof(GameplaySystemGroup))]
    public class UISystemGroup : ComponentSystemGroup { }
}