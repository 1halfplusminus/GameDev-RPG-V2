using Unity.Animation;
using Unity.Entities;
using UnityEngine;
using RPG.Core;
using Unity.Mathematics;
using Unity.Collections;

namespace RPG.Animation
{
    public struct ChangeAttackAnimation : IComponentData
    {
        public BlobAssetReference<Clip> Animation;
    }
    public class ClipPlayerConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ClipPlayer cp) =>
            {
                if (cp.Clip == null)
                {
                    return;
                }
                Entity entity = GetPrimaryEntity(cp);
                DynamicBuffer<AnimationClips> buffer;
                if (!DstEntityManager.HasComponent<AnimationClips>(entity))
                {
                    buffer = DstEntityManager.AddBuffer<AnimationClips>(entity);
                }
                else
                {
                    buffer = DstEntityManager.GetBuffer<AnimationClips>(entity, false); ;
                }
                DeclareAssetDependency(cp.gameObject, cp.Clip);
                var denseClip = cp.Clip.GetClip();
                if (!denseClip.IsCreated)
                {
                    UnityEngine.Debug.LogWarning($"Unable to load clip {cp.Clip.name}");
                }
                _ = buffer.Add(new AnimationClips
                {
                    Clip = denseClip
                });
                DstEntityManager.AddComponentData(entity, new PlayClip() { Index = buffer.Length - 1 });
                DstEntityManager.AddComponent<DeltaTime>(entity);
            });
        }
    }

    public class ClipPlayer : MonoBehaviour
    {

        public ClipAsset Clip;
    }

    public struct PlayClip : IComponentData
    {
        public int Index;

        public int PreviousClip;
        public float Weight;

        public bool DontLoop;
    }
    [UpdateAfter(typeof(CharacterAnimationSystem))]
    [UpdateBefore(typeof(AnimationCoreSystem))]
    public partial class PlayClipSystem : SystemBase
    {
        public float TotalTime = 0;
        protected override void OnUpdate()
        {
            TotalTime += UnityEngine.Time.deltaTime;
            var time = TotalTime;
            Entities.ForEach((
            ref DynamicBuffer<AnimatedData> animatedDatas,
            ref AnimationStreamComponent streamComponent,
            in DynamicBuffer<AnimationClips> animationClips,
            in Rig rigRef,
            in PlayClip playClip) =>
            {
                if (streamComponent.Value.IsNull)
                {
                    return;
                }
                var rig = rigRef.Value;
                if (animationClips[playClip.Index].ClipInstance.IsCreated)
                {
                    BlobAssetReference<Clip> animationClip = animationClips[playClip.Index].Clip;
                    float normalizedT = NormalizedTime(time, animationClip, !playClip.DontLoop);
                    var buffer1 = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
                    ref AnimationStream stream = ref streamComponent.Value;
                    stream.ClearMasks();
                    var stream1 = AnimationStream.Create(rigRef.Value, buffer1);
                    Unity.Animation.Core.EvaluateClip(animationClips[playClip.Index].ClipInstance, normalizedT, ref stream1, 1);
                    buffer1.Dispose();
                    var buffer2 = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
                    var stream2 = AnimationStream.Create(rigRef.Value, buffer2);
                    normalizedT = NormalizedTime(time, animationClips[playClip.PreviousClip].Clip, !playClip.DontLoop);
                    Unity.Animation.Core.EvaluateClip(animationClips[playClip.PreviousClip].ClipInstance, normalizedT, ref stream2, 1);
                    Unity.Animation.Core.Blend(ref stream, ref stream2, ref stream1, playClip.Weight);
                    buffer2.Dispose();
                }
            }).ScheduleParallel();
        }

        public static float NormalizedTime(float time, BlobAssetReference<Clip> animationClip, bool loop)
        {
            if (loop)
            {
                var normalizedTime = time / animationClip.Value.Duration;
                var normalizedTimeInt = (int)normalizedTime;

                var cycle = math.select(normalizedTimeInt, normalizedTimeInt - 1, normalizedTime < 0);
                normalizedTime = math.select(normalizedTime - normalizedTimeInt, normalizedTime - normalizedTimeInt + 1, normalizedTime < 0);
                return normalizedTime * animationClip.Value.Duration;
            }
            return time;
        }
    }

}
