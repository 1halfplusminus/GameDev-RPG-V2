using Unity.Entities;
using UnityEngine.UIElements;
using static Unity.Entities.ComponentType;
using RPG.Gameplay;

namespace RPG.UI
{
    public struct RenderDialog : IComponentData
    {
        public Entity DialogEntity;
        public BlobAssetReference<BlobDialog> DialogAsset;
    }
    public struct DialogInstance : IComponentData
    {
        public Entity Instance;
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class DialogUISystem : SystemBase
    {
        EntityQuery dialogUIPrefabQuery;
        EntityQuery dialogUIQuery;

        EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            dialogUIPrefabQuery = GetEntityQuery(ReadOnly<Prefab>(), ReadOnly<DialogUI>());
            dialogUIQuery = GetEntityQuery(ReadOnly<RenderDialog>());
            endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            var dialogUIPrefab = dialogUIPrefabQuery.GetSingletonEntity();
            Entities
            .WithNone<DialogInstance>().ForEach((int entityInQueryIndex, Entity e, Dialog dialog) =>
            {
                var instance = cbp.Instantiate(entityInQueryIndex, dialogUIPrefab);
                cbp.AddComponent(entityInQueryIndex, instance, new RenderDialog { DialogAsset = dialog.Reference, DialogEntity = e });
                cbp.AddComponent(entityInQueryIndex, e, new DialogInstance { Instance = instance });
            }).ScheduleParallel();

            Entities
            .WithAll<DialogUI, UIReady>()
            .WithNone<DialogController>()
            .ForEach((UIDocument document, Entity e, RenderDialog displayDialog) =>
            {
                var controller = new DialogController();
                controller.Init(document.rootVisualElement);
                controller.ShowNode(displayDialog.DialogAsset, displayDialog.DialogAsset.Value.StartIndex);
                EntityManager.AddComponentData(e, controller);
                EntityManager.AddComponentData(displayDialog.DialogEntity, controller);
            })
            .WithoutBurst()
            .WithStructuralChanges()
            .Run();


            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

}