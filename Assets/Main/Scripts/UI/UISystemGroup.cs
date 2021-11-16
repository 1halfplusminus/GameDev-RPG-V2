using RPG.Gameplay;
using Unity.Entities;

namespace RPG.UI
{
    [UpdateAfter(typeof(GameplaySystemGroup))]
    public class UISystemGroup : ComponentSystemGroup { }
}