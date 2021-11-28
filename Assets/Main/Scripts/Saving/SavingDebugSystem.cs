using RPG.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RPG.Saving
{
    [UpdateInGroup(typeof(SavingSystemGroup))]
    public class SavingDebugSystem : SystemBase
    {

        SaveSystem saveSystem;
        EntityQuery requestForUpdateQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            saveSystem = World.GetOrCreateSystem<SaveSystem>();
            requestForUpdateQuery = GetEntityQuery(new EntityQueryDesc()
            {
                None = new ComponentType[] {
                    typeof(TriggerSceneLoad),
                    typeof(TriggeredSceneLoaded),
                    typeof(LoadSceneAsync),
                    typeof(UnloadScene),
                    typeof(AnySceneLoading)
                }
            });
            RequireForUpdate(requestForUpdateQuery);
        }
        protected override void OnUpdate()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.altKey.isPressed && keyboard.sKey.isPressed)
                {
                    Debug.Log("Saving in file");
                    //FIXME: Save should not be called directly the save system should react to a component that request a save
                    saveSystem.Save();
                }
                if (keyboard.altKey.isPressed && keyboard.lKey.isPressed)
                {
                    Debug.Log("Loading from file");
                    //FIXME: Load should not be called directly the save system should react to a component that request a Load
                    saveSystem.Load();
                }
            }
        }
    }
}
