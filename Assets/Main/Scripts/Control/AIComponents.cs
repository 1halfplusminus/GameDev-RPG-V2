
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RPG.Control
{
    public struct AIControlled : IComponentData { }

    public struct ChasePlayer : IComponentData
    {
        public float ChaseDistance;
    }

    public struct GuardOriginalLocationTag : IComponentData { }
    public struct GuardLocation : IComponentData
    {
        public float3 Value;

        public float SuspiciousTime;
    }
    public struct IsSuspicious : IComponentData { }

    public struct Suspicious : IComponentData
    {

        public float Time;
        float currentTime;

        bool startedThisFrame;
        bool started;
        bool finish;
        public bool StartedThisFrame { get { return startedThisFrame; } }

        public bool IsStarted { get { return started; } }

        public bool IsFinish { get { return finish; } }
        public Suspicious(float time)
        {
            Time = time;
            currentTime = math.EPSILON;
            startedThisFrame = false;
            started = false;
            finish = false;
        }

        public void Reset()
        {
            currentTime = math.EPSILON;
            startedThisFrame = false;
            started = false;
            finish = false;
        }
        public void Finish()
        {
            finish = true;
        }
        public void Start()
        {
            currentTime = Time;
            startedThisFrame = true;
            started = true;
            finish = false;
        }
        public void Update(float deltaTime)
        {
            currentTime -= deltaTime;
            startedThisFrame = false;
            if (currentTime <= 0)
            {
                finish = true;
            }
        }
    }
}