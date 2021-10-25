using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using Unity.Physics;
using static ExtensionMethods.EcsConversionExtension;
using UnityEngine.InputSystem.Utilities;


namespace RPG.Core
{
    [GenerateAuthoringComponent]
    public struct MouseClick : IComponentData
    {
        public float3 ScreenCordinate;
        public Unity.Physics.Ray Ray;

        public bool CapturedThisFrame;
    }

    public class MouseInputSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        GameInput input;

        MouseClick capturedClick;

        protected override void OnCreate()
        {
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            input = new GameInput();
            input.Enable();
        }
        protected override void OnDestroy()
        {
            input.Disable();
        }
        protected MouseClick ReadClick()
        {


            float2 value = Pointer.current.position.ReadValue();
            var ray = FromEngineRay(Camera.main.ScreenPointToRay(new float3(value, 0f)));
            capturedClick = new MouseClick { ScreenCordinate = ray.Origin, Ray = ray, CapturedThisFrame = true };
            return capturedClick;
        }
        protected override void OnUpdate()
        {
            var controller = input.Gameplay.Click.activeControl;
            if (controller != null && controller.IsPressed())
            {
                var CapturedClick = ReadClick();
                var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
                Entities.ForEach((Entity e, int entityInQueryIndex, in MouseClick m) =>
                {
                    commandBuffer.SetComponent(entityInQueryIndex, e, CapturedClick);
                }).Schedule();
                entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
            }


        }
    }


    public class DebugPlayerMouseInputSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var camera = Camera.main;
            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((ref MouseClick c) =>
            {
                Debug.DrawRay(c.Ray.Origin, c.Ray.Displacement * 100f, Color.red, 1f);
            }).Run();
        }
    }

}
