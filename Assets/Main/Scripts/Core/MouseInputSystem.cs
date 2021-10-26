
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using Unity.Physics;
using static ExtensionMethods.EcsConversionExtension;


namespace RPG.Core
{
    [GenerateAuthoringComponent]
    public struct MouseClick : IComponentData
    {
        public float3 ScreenCordinate;
        public Unity.Physics.Ray Ray;

        public bool CapturedThisFrame;
        public bool MovedThisFrame;
        public int Frame;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]

    public class MouseInputSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        GameInput input;

        MouseClick capturedClick;

        protected override void OnCreate()
        {
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            input = new GameInput();
            input.Enable();
        }
        protected override void OnDestroy()
        {
            input.Disable();
        }
        protected MouseClick ReadClick()
        {

            var controller = input.Gameplay.Click.activeControl;
            var capturedThisFrame = controller != null && controller.IsPressed();
            float2 value = Pointer.current.position.ReadValue();
            var ray = FromEngineRay(Camera.main.ScreenPointToRay(new float3(value, 0f)));
            capturedClick = new MouseClick { ScreenCordinate = ray.Origin, Ray = ray, CapturedThisFrame = capturedThisFrame };
            return capturedClick;
        }
        protected override void OnUpdate()
        {
            var CapturedClick = ReadClick();
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.ForEach((Entity e, int entityInQueryIndex, in MouseClick m) =>
            {
                if (!CapturedClick.CapturedThisFrame)
                {
                    CapturedClick.Frame = m.Frame;
                }
                commandBuffer.SetComponent(entityInQueryIndex, e, CapturedClick);
            }).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);

        }
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    [UpdateBefore(typeof(MouseInputSystem))]
    public class EndSimulationMouseClickSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Mark old click as not captured this frame

            Entities.ForEach((ref MouseClick click) =>
            {
                if (click.Frame >= 1)
                {
                    click.CapturedThisFrame = false;
                }
                click.Frame += 1;
            }).ScheduleParallel();
        }
    }

    public class DebugPlayerMouseInputSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var camera = Camera.main;
            Entities
            .WithoutBurst()
            .ForEach((ref MouseClick c) =>
            {
                Debug.DrawRay(c.Ray.Origin, c.Ray.Displacement * 100f, Color.red, 1f);
            }).Run();
        }
    }

}
