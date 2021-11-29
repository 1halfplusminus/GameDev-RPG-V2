
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using RPG.Control;
using RPG.Core;
using RPG.Mouvement;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RPG.Saving
{
    public interface ISerializer
    {
        object Serialize(EntityManager em, Entity e);
        void UnSerialize(EntityManager em, Entity e, object state);
    }
    public struct Saveable : IComponentData
    {
        public FixedList128<ComponentType> types;
    }
    public struct CharacterSerialisation : ISerializer
    {
        public object Serialize(EntityManager em, Entity e)
        {
            Debug.Log($"Serialize {e}");
            return em.GetComponentData<Translation>(e);
        }

        public void UnSerialize(EntityManager em, Entity e, object state)
        {
            Debug.Log($"Unserialize ${e}");
            if (state is Translation translation)
            {
                em.AddComponentData(e, translation);
                em.AddComponentData(e, new WarpTo() { Destination = translation.Value });
            }

        }
    }

    [UpdateInGroup(typeof(SavingSystemGroup))]
    public class SavingDebugSystem : SystemBase
    {
        Dictionary<EntityQueryMask, ISerializer> serializers;
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
            serializers = new Dictionary<EntityQueryMask, ISerializer>();
            serializers.Add(GetEntityQuery(typeof(Translation)).GetEntityQueryMask(), new CharacterSerialisation());
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
            var state = formatter.Deserialize(stream);
            RestoreState(state);
        }
        private object CaptureState()
        {
            var state = new Dictionary<string, object>();
            var queryIdentifier = GetEntityQuery(typeof(Identifier), typeof(Saveable));
            using var identifieds = queryIdentifier.ToComponentDataArray<Identifier>(Allocator.Temp);
            using var saveables = queryIdentifier.ToComponentDataArray<Saveable>(Allocator.Temp);
            using var identifiedEntities = queryIdentifier.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < identifiedEntities.Length; i++)
            {
                var entity = identifiedEntities[i];
                var identified = identifieds[i];
                foreach (var serializer in serializers)
                {

                    if (serializer.Key.Matches(entity))
                    {
                        var r = serializer.Value.Serialize(EntityManager, entity);
                        state.Add(identified.Id.ToString(), r);
                        break;
                    }
                }

            }

            return state;
        }
        private void RestoreState(object state)
        {
            Dictionary<string, object> stateDict = (Dictionary<string, object>)state;
            var queryIdentifier = GetEntityQuery(typeof(Identifier), typeof(Saveable));
            using var identifieds = queryIdentifier.ToComponentDataArray<Identifier>(Allocator.Temp);
            using var saveables = queryIdentifier.ToComponentDataArray<Saveable>(Allocator.Temp);
            using var identifiedEntities = queryIdentifier.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < identifiedEntities.Length; i++)
            {
                var entity = identifiedEntities[i];
                foreach (var serializer in serializers)
                {

                    if (serializer.Key.Matches(entity))
                    {
                        serializer.Value.UnSerialize(EntityManager, entity, stateDict[identifieds[i].Id.ToString()]);
                        break;
                    }
                }
            }
        }
        private void SaveUdemyCourse()
        {
            Debug.Log($"Writing to {savePath}");
            using var stream = File.Open(savePath, FileMode.Create);
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, CaptureState());
        }

    }
}
