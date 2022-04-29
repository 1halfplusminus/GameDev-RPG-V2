using RPG.Core;
using Unity.Animation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace RPG.Animation
{

    public struct CharacterAnimationSetup : IComponentData
    {
        public int IDLE;

        public int Walk;

        public int Run;

        public int Attack;

        public int Dead;
    }

    class CharacterAnimationAuthoring : MonoBehaviour
    {
        public ClipAsset IDLE;

        public ClipAsset Walk;

        public ClipAsset Run;

        public ClipAsset Attack;

        public ClipAsset Dead;
    }

    public class CharacterAnimationConversionSystem : GameObjectConversionSystem
    {
        protected int AddClip(BlobAssetReference<Clip> clip, ref DynamicBuffer<AnimationClips> clips)
        {
            clips.Add(new AnimationClips { Clip = clip });
            return clips.Length - 1;
        }
        protected override void OnUpdate()
        {
            Entities.ForEach((CharacterAnimationAuthoring characterAnimation) =>
            {
                var entity = GetPrimaryEntity(characterAnimation);
                var setup = new CharacterAnimationSetup { };
                var clipBuffer = DstEntityManager.HasComponent<AnimationClips>(entity) ?
                DstEntityManager.GetBuffer<AnimationClips>(entity) : DstEntityManager.AddBuffer<AnimationClips>(entity);

                if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.IDLE, out var idleClip))
                {
                    setup.IDLE = AddClip(idleClip, ref clipBuffer);
                }
                if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Walk, out var walkClip))
                {
                    setup.Walk = AddClip(walkClip, ref clipBuffer);
                }
                if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Run, out var runClip))
                {
                    setup.Run = AddClip(runClip, ref clipBuffer);
                }
                if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Attack, out var attackClip))
                {
                    setup.Attack = AddClip(attackClip, ref clipBuffer);
                }
                if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Dead, out var deadClip))
                {
                    setup.Dead = AddClip(deadClip, ref clipBuffer);
                }
                DstEntityManager.AddComponent<CharacterAnimation>(entity);
                DstEntityManager.AddComponentData(entity, setup);
                DstEntityManager.AddComponentData(entity, new PlayClip { Index = setup.IDLE });
            });
        }

    }
    [BurstCompile]
    public struct CharacterAnimationSystem : ISystem
    {

        struct ChangeCharacterAnimationJob : IJobEntityBatch
        {
            [ReadOnly]
            public ComponentTypeHandle<CharacterAnimation> CharacterAnimationTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<CharacterAnimationSetup> CharacterAnimationSetupTypeHandle;

            public ComponentTypeHandle<PlayClip> PlayClipTypeHandle;
            public uint LastSystemVersion;

            [BurstCompile]
            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var characterAnimationChanged = batchInChunk.DidChange(CharacterAnimationTypeHandle, LastSystemVersion);
                if (!characterAnimationChanged)
                {
                    return;
                }
                var characterAnimationSetups = batchInChunk.GetNativeArray(CharacterAnimationSetupTypeHandle);
                var characterAnimations = batchInChunk.GetNativeArray(CharacterAnimationTypeHandle);
                var playClips = batchInChunk.GetNativeArray(PlayClipTypeHandle);
                for (int i = 0; i < characterAnimations.Length; i++)
                {
                    var previousClip = playClips[i];
                    PlayClip playClip;
                    if (characterAnimations[i].Dead > 0)
                    {
                        playClip = new PlayClip { Index = characterAnimationSetups[i].Dead, DontLoop = true, Weight = characterAnimations[i].Dead };
                    }
                    else if (characterAnimations[i].Attack > 0)
                    {
                        playClip = new PlayClip { Index = characterAnimationSetups[i].Attack, Weight = characterAnimations[i].Attack };
                    }
                    else if (characterAnimations[i].Run > 0)
                    {
                        playClip = new PlayClip { Index = characterAnimationSetups[i].Run, Weight = characterAnimations[i].Run };
                    }
                    else if (characterAnimations[i].Move > 0)
                    {
                        playClip = new PlayClip { Index = characterAnimationSetups[i].Run, Weight = characterAnimations[i].Move };
                    }
                    else
                    {
                        playClip = new PlayClip { Index = 0, Weight = 1 };
                    }
                    if (playClip.Index != previousClip.Index)
                    {
                        playClip.PreviousClip = previousClip.Index;
                    }
                    playClips[i] = playClip;
                }

            }
        }
        private EntityQuery queryCharacterAnimation;
        public void OnCreate(ref SystemState state)
        {
            queryCharacterAnimation = state.GetEntityQuery(
                ComponentType.ReadOnly<CharacterAnimation>(),
                ComponentType.ReadOnly<CharacterAnimationSetup>(),
                ComponentType.ReadWrite<PlayClip>()
            );
            state.RequireForUpdate(queryCharacterAnimation);
        }

        public void OnDestroy(ref SystemState state)
        {

        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new ChangeCharacterAnimationJob
            {
                CharacterAnimationSetupTypeHandle = state.GetComponentTypeHandle<CharacterAnimationSetup>(),
                CharacterAnimationTypeHandle = state.GetComponentTypeHandle<CharacterAnimation>(),
                PlayClipTypeHandle = state.GetComponentTypeHandle<PlayClip>(),
                LastSystemVersion = state.LastSystemVersion
            }.ScheduleParallel(queryCharacterAnimation, state.Dependency);
        }
    }
}
