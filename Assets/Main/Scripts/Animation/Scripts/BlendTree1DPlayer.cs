using Unity.Entities;
using Unity.Animation;
using UnityEngine;

namespace RPG.Animation
{
#if UNITY_EDITOR
    using UnityEditor.Animations;
    using Unity.Animation.Hybrid;

    class BlendTree1DPlayer : MonoBehaviour
    {

        public BlendTree BlendTree;

    }
    [UpdateAfter(typeof(RigConversion))]
    public class BlendTree1DPlayerConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((BlendTree1DPlayer player) =>
            {
                var entity = GetPrimaryEntity(player);
                var rigDefinition = DstEntityManager.GetComponentData<Rig>(entity);
                var clipConfiguration = new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime };
                var bakeOptions = new BakeOptions
                {
                    RigDefinition = rigDefinition.Value,
                    ClipConfiguration = clipConfiguration,
                    SampleRate = 60f
                };
                var blendTreeIndex = BlendTreeConversion.Convert(player.BlendTree, entity, DstEntityManager, bakeOptions);
                var graphSetup = new BlendTree1DSetup
                {
                    BlendTreeIndex = blendTreeIndex,
                };
                DstEntityManager.AddComponentData(entity, graphSetup);
                DstEntityManager.AddComponent<DeltaTime>(entity);

            });
        }
    }
#endif
}

