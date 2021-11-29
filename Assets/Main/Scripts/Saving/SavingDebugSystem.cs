
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using RPG.Control;
using RPG.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RPG.Saving
{

    [UpdateInGroup(typeof(SavingSystemGroup))]
    public class SavingDebugSystem : SystemBase
    {

        SaveSystem saveSystem;
        EntityQuery requestForUpdateQuery;

        string savePath;
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
                    saveSystem.Save();
                    SaveUdemyCourse();
                }
                if (keyboard.altKey.isPressed && keyboard.lKey.wasPressedThisFrame)
                {
                    //FIXME: Load should not be called directly the save system should react to a component that request a Load
                    saveSystem.Load();
                    LoadUdemyCourse();
                }
            }
        }

        private void LoadUdemyCourse()
        {
            Debug.Log($"Loading from file {savePath}");
            using var stream = File.Open(savePath, FileMode.Open);
            var formatter = new BinaryFormatter();
            var result = formatter.Deserialize(stream);
            if (result is Translation position)
            {
                Debug.Log($"Deserialized player position: {position.Value}");

            }
        }

        private void GetHolaMundoInUTF8()
        {
            /*  var toWrite = new byte[] {
                    0xc2,0xa1,0x48,0x6f,0x6c,0x6c,0x61,0x20,0x6d,0x75,0x6e,0x64,0x6f,0x21
            }; */
            // var toWrite = Encoding.UTF8.GetBytes("¡Hola Mundo!");
        }
        private void SaveUdemyCourse()
        {
            Debug.Log($"Writing to {savePath}");
            using var stream = File.Open(savePath, FileMode.Create);
            var queryPlayer = GetEntityQuery(typeof(PlayerControlled), typeof(Translation));
            var playerEntity = queryPlayer.GetSingletonEntity();
            var playerPosition = queryPlayer.GetSingleton<Translation>();

            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, playerPosition);
        }

        protected byte[] SavePlayerPosition(float3 position)
        {
            var buffer = new byte[4 * 3];
            BitConverter.GetBytes(position.x).CopyTo(buffer, 0);
            BitConverter.GetBytes(position.y).CopyTo(buffer, 4);
            BitConverter.GetBytes(position.z).CopyTo(buffer, 8);
            return buffer;
        }

        protected float3 DeserializePosition(byte[] bytes)
        {
            var position = new float3
            {
                x = BitConverter.ToSingle(bytes, 0),
                y = BitConverter.ToSingle(bytes, 4),
                z = BitConverter.ToSingle(bytes, 8)
            };
            return position;
        }
    }
}
