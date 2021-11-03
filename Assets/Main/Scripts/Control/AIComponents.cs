
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RPG.Control
{
    public struct AIControlled : IComponentData { }

    public struct ChasePlayer : IComponentData
    {
        public float ChaseDistance;
        public Entity Target;
    }

    public struct GuardOriginalLocationTag : IComponentData { }
    public struct GuardLocation : IComponentData
    {
        public float3 Value;

        public float SuspiciousTime;
    }

    public struct ChaseTargetLose : IComponentData
    {
        public Entity Target;

    }
    public struct StartChaseTarget : IComponentData
    {
        public Entity Target;

        public float3 Position;
    }


    public struct IsChasingTarget : IComponentData { }

    public struct IsPatroling : IComponentData { }
    public struct IsSuspicious : IComponentData { }

    public struct PatrolWaypoint : IBufferElementData
    {
        public float3 Position;
    }
    public struct PatrollingPath : IComponentData
    {
        public Entity Entity;
    }

    public struct Patrolling : IComponentData
    {

        public int WayPointCount;

        public float PatrolSpeed;
        public float StopingDistance;

        public int _currentWayPointIndex;

        public bool _started;

        public bool _stopped;

        public float _distanceToWaypoint;

        public int CurrentWayPoint { get => _currentWayPointIndex; }

        public bool Started { get => _started; }


        public Patrolling(float patrolSpeed)
        {
            PatrolSpeed = patrolSpeed;
            WayPointCount = 0;
            StopingDistance = 10.0f;
            _distanceToWaypoint = math.INFINITY;
            _currentWayPointIndex = 0;
            _started = false;
            _stopped = false;
        }
        public void Start(int waypointCount)
        {

            WayPointCount = waypointCount;
            _distanceToWaypoint = math.INFINITY;
            _started = true;
        }
        public void Stop()
        {
            _started = false;
            _stopped = true;
        }
        public void Reset()
        {
            _started = false;
            _stopped = false;
            _currentWayPointIndex = 0;
            _distanceToWaypoint = math.INFINITY;
        }
        public void Update(float3 currentPosition, float3 currentWaypoint)
        {
            if (!_started) { return; }
            _distanceToWaypoint = math.abs(math.distance(currentWaypoint, currentPosition));
            if (_distanceToWaypoint <= StopingDistance)
            {
                _distanceToWaypoint = math.INFINITY;
                Next();
            }

        }
        public void Next()
        {
            if (!_started) { return; }
            if (_currentWayPointIndex + 1 == WayPointCount)
            {
                _currentWayPointIndex = 0;
            }
            else
            {
                _currentWayPointIndex++;
            }
        }


    }
    public struct Suspicious : IComponentData
    {

        public float Time;
        public float _currentTime;

        bool startedThisFrame;
        bool started;
        bool finish;
        public bool StartedThisFrame { get { return startedThisFrame; } }

        public bool IsStarted { get { return started; } }

        public bool IsFinish { get { return finish; } }
        public Suspicious(float time)
        {
            Time = time;
            _currentTime = math.INFINITY;
            startedThisFrame = false;
            started = false;
            finish = false;
        }

        public void Reset()
        {
            _currentTime = math.INFINITY;
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
            _currentTime = Time;
            startedThisFrame = true;
            started = true;
            finish = false;
        }
        public void Update(float deltaTime)
        {
            if (!started) { return; }
            _currentTime -= deltaTime;
            startedThisFrame = false;
            if (_currentTime <= 0)
            {
                finish = true;
            }
        }
    }
}