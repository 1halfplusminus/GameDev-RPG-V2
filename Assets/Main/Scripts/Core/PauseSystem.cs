
//FIXME: Move to UI System
// using System.Collections.Generic;
// using RPG.Combat;
// using RPG.Mouvement;
// using Unity.Animation;
// using Unity.Entities;

// namespace RPG.Core
// {
//     [UpdateInGroup(typeof(CoreSystemGroup))]
//     public partial class PauseSystem : SystemBase
//     {
//         List<ComponentSystemGroup> pausableSystemGroup;
//         protected override void OnCreate()
//         {
//             base.OnCreate();
//             pausableSystemGroup = new List<ComponentSystemGroup>();
//             //FIXME: Dependancy should be inversed, should load the system from all system group that implement ipausable
//             pausableSystemGroup.Add(World.GetExistingSystem<MouvementSystemGroup>());
//             pausableSystemGroup.Add(World.GetExistingSystem<DefaultAnimationSystemGroup>());
//             pausableSystemGroup.Add(World.GetExistingSystem<CombatSystemGroup>());
//         }
//         protected override void OnUpdate()
//         {

//         }

//         public void Pause(bool paused)
//         {
//             foreach (var item in pausableSystemGroup)
//             {
//                 item.Enabled = !paused;
//             }
//         }
//     }
// }