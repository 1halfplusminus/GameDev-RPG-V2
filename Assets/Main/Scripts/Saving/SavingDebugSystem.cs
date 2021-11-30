
using System;
using System.Text;
using RPG.Control;
using RPG.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RPG.Saving
{
    [UpdateInGroup(typeof(SavingSystemGroup))]
    public class SavingDebugSystem : SystemBase
    {

        SaveSystemBase saveSystem;
        EntityQuery requestForUpdateQuery;

        string savePath;
        protected override void OnCreate()
        {
            base.OnCreate();
            saveSystem = World.GetOrCreateSystem<SaveSystemBase>();
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
            savePath = SaveSystem.GetPathFromSaveFile("test.save");

            RequireForUpdate(requestForUpdateQuery);

        }
        protected override void OnUpdate()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.altKey.isPressed && keyboard.sKey.wasPressedThisFrame)
                {
                    Debug.Log("Saving in file");
                    //FIXME: Save should not be called directly the save system should react to a component that request a save
                    // saveSystem.Save();
                    saveSystem.Save(savePath);
                }
                if (keyboard.altKey.isPressed && keyboard.lKey.wasPressedThisFrame)
                {
                    //FIXME: Load should not be called directly the save system should react to a component that request a Load
                    // saveSystem.Load();
                    saveSystem.Load(savePath);
                }
            }
        }

    }
}

