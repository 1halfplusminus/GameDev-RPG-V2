using Unity.Entities;
using RPG.Control;

namespace RPG.Gameplay
{

    [UpdateAfter(typeof(ControlSystemGroup))]
    public class GameplaySystemGroup : ComponentSystemGroup { }
}
