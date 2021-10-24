using Unity.Entities;
using Unity.Animation;
using Unity.Collections;
using Unity.DataFlowGraph;

public interface IAnimationSetup: IComponentData {}
public interface IAnimationData: ISystemStateComponentData {};


public abstract class AnimationSystemBase<TAnimationSetup,TAnimationData, TAnimationSystem>
: ComponentSystem 
where TAnimationSetup: struct, IAnimationSetup
where TAnimationData: struct, IAnimationData
where TAnimationSystem: SystemBase, IAnimationGraphSystem {
    protected TAnimationSystem animationSystem;

    EntityQueryBuilder.F_EDD<Rig, TAnimationSetup> createLambda;

    EntityQueryBuilder.F_ED<TAnimationData> destroyLambda;

    protected override void OnCreate() {
        base.OnCreate();
        animationSystem = World.GetOrCreateSystem<TAnimationSystem>();
        animationSystem.AddRef();
        animationSystem.Set.RendererModel = NodeSet.RenderExecutionModel.Islands;

        createLambda = (Entity e, ref Rig rig,ref TAnimationSetup setup) =>
        {
            var data = CreateGraph(e,ref rig, animationSystem, ref setup);
            PostUpdateCommands.AddComponent(e,data);
        };

        destroyLambda = (Entity e, ref TAnimationData data) => {
            DestroyGraph(e, animationSystem, ref data);
            PostUpdateCommands.RemoveComponent<TAnimationData>(e);
        };
    }

    protected override void OnDestroy() {
        if(animationSystem == null) {
            return;
        }
        var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
        Entities.ForEach((Entity e, ref TAnimationData data)=> {
            DestroyGraph(e,animationSystem,ref data);
            cmdBuffer.RemoveComponent<TAnimationData>(e);
        });

        cmdBuffer.Playback(EntityManager);
        cmdBuffer.Dispose();

        animationSystem.RemoveRef();

        base.OnDestroy();
    }

    protected override void OnUpdate() {

        // Create graph
        Entities.WithNone<TAnimationData>().ForEach(createLambda);

        // DestroyGraph
        Entities.WithNone<TAnimationSetup>().ForEach(destroyLambda);
        
    }
    protected abstract TAnimationData CreateGraph(Entity e, ref Rig rig, TAnimationSystem graphSystem, ref TAnimationSetup setup);

    protected abstract void DestroyGraph(Entity e, TAnimationSystem graphSystem, ref TAnimationData data);
}