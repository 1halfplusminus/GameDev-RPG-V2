
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RPG.Control
{

    public struct AIControlled : IComponentData { }

    public struct ChasePlayer : IComponentData
    {
        public float AngleOfView;
        public float ChaseDistance;
        public float ChaseDistanceSq;
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
        public float DwellingTime;
        public float StopingDistance;

        public int _currentWayPointIndex;

        public bool _started;

        public bool _stopped;

        public float _distanceToWaypoint;

        public bool _isDwelling;

        public int CurrentWayPoint { get => _currentWayPointIndex; }

        public bool Started { get => _started && !_stopped; }

        public bool IsDwelling { get => _isDwelling; }
        public Patrolling(float patrolSpeed)
        {
            PatrolSpeed = patrolSpeed;
            WayPointCount = 0;
            StopingDistance = 10.0f;
            DwellingTime = math.EPSILON;
            _distanceToWaypoint = math.INFINITY;
            _currentWayPointIndex = 0;
            _started = false;
            _stopped = false;
            _isDwelling = false;
        }
        public void Start(int waypointCount)
        {

            WayPointCount = waypointCount;
            _distanceToWaypoint = math.INFINITY;
            _started = true;
            _isDwelling = false;
        }
        public void Stop()
        {

            _stopped = true;
        }

        public void UnStop()
        {
            _stopped = false;
        }
        public void Toggle()
        {
            _stopped = !_stopped;

        }
        public void Reset()
        {
            _started = false;
            _stopped = false;
            _isDwelling = false;
            _currentWayPointIndex = 0;
            _distanceToWaypoint = math.INFINITY;
        }
        public void Update(float3 currentPosition, float3 currentWaypoint, out bool wasDwelling)
        {
            wasDwelling = _isDwelling;
            Update(currentPosition, currentWaypoint);

        }
        public void Update(float3 currentPosition, float3 currentWaypoint)
        {
            if (!Started) { return; }
            _distanceToWaypoint = DistanceToWaypoint(currentPosition, currentWaypoint);
            if (_isDwelling)
            {
                _isDwelling = false;
                return;
            }
            if (_distanceToWaypoint <= StopingDistance)
            {
                _isDwelling = true;
                _distanceToWaypoint = math.INFINITY;
                Next();
            }
        }

        private static float DistanceToWaypoint(float3 currentPosition, float3 currentWaypoint)
        {
            return math.abs(math.distance(currentWaypoint, currentPosition));
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

        public bool _startedThisFrame;
        public bool _started;
        public bool _finish;
        public bool StartedThisFrame { get { return _startedThisFrame; } }

        public bool IsStarted { get { return _started; } }

        public bool IsFinish { get { return _finish; } }
        public Suspicious(float time)
        {
            Time = time;
            _currentTime = math.INFINITY;
            _startedThisFrame = false;
            _started = false;
            _finish = false;
        }

        public void Reset()
        {
            _currentTime = math.INFINITY;
            _startedThisFrame = false;
            _started = false;
            _finish = false;
        }
        public void Finish()
        {
            _finish = true;
        }
        public void Start()
        {
            Start(Time);
        }
        public void Start(float time)
        {
            _currentTime = time;
            _startedThisFrame = true;
            _started = true;
            _finish = false;
        }
        public void Update(float deltaTime)
        {
            if (!_started) { return; }
            _currentTime -= deltaTime;
            _startedThisFrame = false;
            if (_currentTime <= 0)
            {
                _finish = true;
            }
        }
    }
}