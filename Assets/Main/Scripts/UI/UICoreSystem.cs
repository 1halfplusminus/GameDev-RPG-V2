using RPG.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.UI
{

    [UpdateInGroup(typeof(UISystemGroup))]
    [UpdateBefore(typeof(UICoreSystem))]
    public partial class InteractWithUISystem : SystemBase
    {
        EntityQuery interactionWithUIEntityQuery;
        EntityQuery rayCastQuery;

        UICoreSystem uiCoreSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            interactionWithUIEntityQuery = GetEntityQuery(typeof(InteractWithUI));
            rayCastQuery = GetEntityQuery(
               new ComponentType[] {
                    ComponentType.ReadOnly<Raycast>(),
                    ComponentType.ReadOnly<HittedByRaycastEvent>()
               }
            );
            uiCoreSystem = World.GetOrCreateSystem<UICoreSystem>();
            RequireForUpdate(rayCastQuery);
        }

        protected override void OnUpdate()
        {
            if (uiCoreSystem.IsPointerOverGameObject)
            {
                EntityManager.AddComponent<InteractWithUI>(rayCastQuery);
            }
            else
            {
                EntityManager.RemoveComponent<InteractWithUI>(rayCastQuery);
            }

        }
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public partial class UICoreSystem : SystemBase
    {
        private bool isPointerOverGameObject;
        public bool IsPointerOverGameObject
        {
            get { return isPointerOverGameObject; }
            private set { isPointerOverGameObject = value; }
        }
        EntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        private void OnMouseOver(MouseOverEvent e)
        {
            isPointerOverGameObject = true;
        }
        private void OnMouseLeave(MouseLeaveEvent e)
        {
            isPointerOverGameObject = false;
        }
        protected override void OnUpdate()
        {
            // isPointerOverGameObject = false;
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            Entities
           .WithNone<UIReady>()
           .WithAll<UIDocument>()
           .ForEach((Entity e, UIDocument uiDocument) =>
           {
               if (uiDocument.rootVisualElement != null)
               {

                   commandBuffer.AddComponent<UIReady>(e);
                   uiDocument.rootVisualElement.RegisterCallback<MouseOverEvent>(OnMouseOver);
                   uiDocument.rootVisualElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
               }
               else
               {
                   //    Debug.LogError("No root visual element");
               }
           })
           .WithoutBurst().Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}