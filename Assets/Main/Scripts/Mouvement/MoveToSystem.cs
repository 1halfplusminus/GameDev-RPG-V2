
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.AI;
using Unity.Mathematics;
using RPG.Core;
using UnityEngine;
using Unity.AI.Navigation;
using Unity.Collections;
using System.Runtime.InteropServices;
using System;
using UnityEngine.Experimental.AI;
using Unity.Collections.LowLevel.Unsafe;
namespace RPG.Mouvement
{
    public class ManagedPath : IComponentData
    {
        public NavMeshPath Path;
        // public GCHandle Handle;
    }

    [UpdateInGroup(typeof(MouvementSystemGroup))]
    public partial class IsMovingSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery isMovingQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithStoreEntityQueryInField(ref isMovingQuery)
            // .WithChangeFilter<MoveTo>()
            .WithNone<IsDeadTag, IsMoving>()
            .ForEach((int entityInQueryIndex, Entity e, in MoveTo moveTo) =>
            {
                if (!moveTo.Stopped)
                {
                    commandBuffer.AddComponent<IsMoving>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    //FIXME: split in smaller system
    [UpdateInGroup(typeof(MouvementSystemGroup))]
    [UpdateAfter(typeof(IsMovingSystem))]
    public partial class MoveToSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {

            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            // Calcule distance 
            Entities
           .WithChangeFilter<MoveTo>()
           .WithNone<IsDeadTag>()
           .WithAny<IsMoving>()
           .ForEach((int entityInQueryIndex, Entity e, ref MoveTo moveTo, in Translation t) =>
           {
               if (!moveTo.UseDirection)
               {
                   moveTo.Distance = math.distance(moveTo.Position, t.Value);
                   if (moveTo.Distance <= moveTo.StoppingDistance)
                   {
                       moveTo.Stopped = true;
                       moveTo.Position = t.Value;
                   }
               }

           }).ScheduleParallel();

            Entities
           //    .WithChangeFilter<MoveTo>()
           .WithAny<IsMoving>()
           .WithNone<IsDeadTag>()
           .ForEach((int entityInQueryIndex, Entity e, ref Mouvement mouvement, in MoveTo moveTo) =>
           {
               if (moveTo.Stopped)
               {
                   commandBuffer.RemoveComponent<IsMoving>(entityInQueryIndex, e);
                   mouvement.Velocity = new Velocity { Linear = float3.zero, Angular = float3.zero };
               }

           }).ScheduleParallel();


            // Initialize move to when spawned
            Entities
            .WithAll<Spawned>()
            .ForEach((ref Translation position, ref MoveTo moveTo) =>
            {
                moveTo.Position = position.Value;
            }).ScheduleParallel();

            Entities
            .WithAll<WarpTo, Warped>()
            .ForEach((int entityInQueryIndex, Entity e) =>
            {
                commandBuffer.RemoveComponent<WarpTo>(entityInQueryIndex, e);
                commandBuffer.RemoveComponent<Warped>(entityInQueryIndex, e);
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

        }
    }
    [UpdateInGroup(typeof(MouvementSystemGroup))]
    [UpdateAfter(typeof(MoveToSystem))]
    public partial class MoveToNavMeshAgentSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery navMeshAgentQueries;

        NavMeshQuery[] navQueries;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            navQueries = new NavMeshQuery[0];
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < navQueries.Length; i++)
            {
                navQueries[i].Dispose();
            }
        }
        protected override void OnUpdate()
        {

            for (int i = 0; i < navQueries.Length; i++)
            {
                navQueries[i].Dispose();
            }
            navQueries = new NavMeshQuery[navMeshAgentQueries.CalculateEntityCount()];
            var unsafeQueries = new UnsafePtrList<IntPtr>(navQueries.Length, Allocator.Temp);
            unsafe
            {
                var ptr = UnsafeUtility.PinGCArrayAndGetDataAddress(navQueries, out var handle);
                var navQueriesCount = navQueries.Length;
                Job.WithCode(() =>
                {
                    for (int i = 0; i < navQueriesCount; i++)
                    {

                        var navMeshWorld = NavMeshWorld.GetDefaultWorld();
                        UnsafeUtility.WriteArrayElement(ptr, i, new NavMeshQuery(navMeshWorld, Allocator.TempJob, 100));
                        ref var navQuery = ref UnsafeUtility.ArrayElementAsRef<NavMeshQuery>(ptr, i);
                        unsafeQueries.Add(UnsafeUtility.AddressOf(ref navQuery));
                    }
                    UnsafeUtility.ReleaseGCObject(handle);
                }).Run();
            }

            // TODO : Refractor get in paralle
            var lookAts = GetComponentDataFromEntity<LookAt>(true);

            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            // Warp when spawned or warp to
            Entities
            .WithAny<Spawned, WarpTo>()
            .ForEach((Entity e, ref MoveTo moveTo, in Translation position) =>
            {
                moveTo.Position = position.Value;
                commandBuffer.AddComponent<Warped>(e);
            }).Schedule();

            var dt = Time.DeltaTime;
            Entities
            .WithDisposeOnCompletion(unsafeQueries)
            .WithAny<IsMoving>()
            .WithNone<IsDeadTag, WarpTo>()
            .WithReadOnly(lookAts)
            .WithStoreEntityQueryInField(ref navMeshAgentQueries)
            .ForEach((
                Entity e,
                int entityInQueryIndex,
                ref Translation position,
                ref Mouvement mouvement,
                ref MoveTo moveTo,
                ref Rotation rotation,
                in LocalToWorld localToWorld
            ) =>
            {
                unsafe
                {
                    var navMeshQuery = UnsafeUtility.AsRef<NavMeshQuery>(unsafeQueries[entityInQueryIndex]);
                    bool isDirection = moveTo.UseDirection;
                    if (isDirection)
                    {
                        moveTo.Direction = math.normalizesafe(moveTo.Direction);
                        var newPosition = moveTo.Direction + (moveTo.StoppingDistance * math.sign(moveTo.Direction));
                        var location = navMeshQuery.MapLocation(
                            (float3)position.Value + newPosition, new Vector3(1f, 1f, 1f), 0, NavMesh.AllAreas);
                        if (navMeshQuery.IsValid(location.polygon))
                        {
                            moveTo.Position = location.position;
                        }
                        moveTo.Direction = float3.zero;
                        moveTo.UseDirection = false;
                    }
                    var start = navMeshQuery.MapLocation(
                           position.Value, new Vector3(1f, 1f, 1f), 0, NavMesh.AllAreas);
                    var end = navMeshQuery.MapLocation(
                          moveTo.Position, new Vector3(1f, 1f, 1f), 0, NavMesh.AllAreas);
                    if (!start.polygon.IsNull() && !end.polygon.IsNull())
                    {
                        int maxIterations = 1024;
                        var status = navMeshQuery.BeginFindPath(start, end);
                        while (status == PathQueryStatus.InProgress)
                        {
                            switch (status)
                            {
                                case PathQueryStatus.InProgress:
                                    status = navMeshQuery.UpdateFindPath(maxIterations, out int currentIterations);
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (status == PathQueryStatus.Success)
                        {
                            navMeshQuery.EndFindPath(out var pathSize);
                            var paths = new NativeArray<PolygonId>(pathSize, Allocator.Temp);
                            var straightPath = new NativeArray<NavMeshLocation>(pathSize + 1, Allocator.Temp);
                            var straightPathFlags = new NativeArray<StraightPathFlags>(pathSize + 1, Allocator.Temp);
                            var vertexSide = new NativeArray<float>(pathSize + 1, Allocator.Temp);
                            navMeshQuery.GetPathResult(paths);
                            int straightPathCount = 0;
                            var pathStatus = PathUtils.FindStraightPath(navMeshQuery, position.Value, moveTo.Position, paths, pathSize, ref straightPath, ref straightPathFlags, ref vertexSide, ref straightPathCount, 100);
                            if (straightPathCount >= 2)
                            {

                                var speed = moveTo.CalculeSpeed(in mouvement);
                                var nextPosition = straightPath[1].position;
                                var direction = (float3)nextPosition - position.Value;
                                var step = speed * dt; // calculate distance to move

                                var moveToward = Vector3.MoveTowards(position.Value, nextPosition, step);
                                direction.y = 0f;
                                var lookAt = Quaternion.LookRotation(direction, math.up());
                                mouvement.Velocity = new Velocity
                                {
                                    Linear = new float3(0, 0, 1f) * mouvement.Speed,
                                    Angular = new float3(0, 0, 0f)
                                };
                                if (!lookAts.HasComponent(e) || lookAts[e].Entity == Entity.Null)
                                {
                                    rotation.Value = lookAt;
                                }
                                position.Value = moveToward;
                            }
                            straightPath.Dispose();
                            straightPathFlags.Dispose();
                            vertexSide.Dispose();
                            paths.Dispose();
                        }

                    }

                }
            }).ScheduleParallel();
            NavMeshWorld.GetDefaultWorld().AddDependency(Dependency);
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

        }
    }

}

public class GeometryUtils
{
    // Calculate the closest point of approach for line-segment vs line-segment.
    public static bool SegmentSegmentCPA(out float3 c0, out float3 c1, float3 p0, float3 p1, float3 q0, float3 q1)
    {
        var u = p1 - p0;
        var v = q1 - q0;
        var w0 = p0 - q0;

        float a = math.dot(u, u);
        float b = math.dot(u, v);
        float c = math.dot(v, v);
        float d = math.dot(u, w0);
        float e = math.dot(v, w0);

        float den = (a * c - b * b);
        float sc, tc;

        if (den == 0)
        {
            sc = 0;
            tc = d / b;

            // todo: handle b = 0 (=> a and/or c is 0)
        }
        else
        {
            sc = (b * e - c * d) / (a * c - b * b);
            tc = (a * e - b * d) / (a * c - b * b);
        }

        c0 = math.lerp(p0, p1, sc);
        c1 = math.lerp(q0, q1, tc);

        return den != 0;
    }
}
[Flags]
public enum StraightPathFlags
{
    Start = 0x01,              // The vertex is the start position.
    End = 0x02,                // The vertex is the end position.
    OffMeshConnection = 0x04   // The vertex is start of an off-mesh link.
}

public class PathUtils
{
    public static float Perp2D(Vector3 u, Vector3 v)
    {
        return u.z * v.x - u.x * v.z;
    }

    public static void Swap(ref Vector3 a, ref Vector3 b)
    {
        var temp = a;
        a = b;
        b = temp;
    }

    // Retrace portals between corners and register if type of polygon changes
    public static int RetracePortals(NavMeshQuery query, int startIndex, int endIndex
        , NativeSlice<PolygonId> path, int n, Vector3 termPos
        , ref NativeArray<NavMeshLocation> straightPath
        , ref NativeArray<StraightPathFlags> straightPathFlags
        , int maxStraightPath)
    {
#if DEBUG_CROWDSYSTEM_ASSERTS
        Assert.IsTrue(n < maxStraightPath);
        Assert.IsTrue(startIndex <= endIndex);
#endif

        for (var k = startIndex; k < endIndex - 1; ++k)
        {
            var type1 = query.GetPolygonType(path[k]);
            var type2 = query.GetPolygonType(path[k + 1]);
            if (type1 != type2)
            {
                Vector3 l, r;
                var status = query.GetPortalPoints(path[k], path[k + 1], out l, out r);

#if DEBUG_CROWDSYSTEM_ASSERTS
                Assert.IsTrue(status); // Expect path elements k, k+1 to be verified
#endif

                float3 cpa1, cpa2;
                GeometryUtils.SegmentSegmentCPA(out cpa1, out cpa2, l, r, straightPath[n - 1].position, termPos);
                straightPath[n] = query.CreateLocation(cpa1, path[k + 1]);

                straightPathFlags[n] = (type2 == NavMeshPolyTypes.OffMeshConnection) ? StraightPathFlags.OffMeshConnection : 0;
                if (++n == maxStraightPath)
                {
                    return maxStraightPath;
                }
            }
        }
        straightPath[n] = query.CreateLocation(termPos, path[endIndex]);
        straightPathFlags[n] = query.GetPolygonType(path[endIndex]) == NavMeshPolyTypes.OffMeshConnection ? StraightPathFlags.OffMeshConnection : 0;
        return ++n;
    }

    public static PathQueryStatus FindStraightPath(NavMeshQuery query, Vector3 startPos, Vector3 endPos
        , NativeSlice<PolygonId> path, int pathSize
        , ref NativeArray<NavMeshLocation> straightPath
        , ref NativeArray<StraightPathFlags> straightPathFlags
        , ref NativeArray<float> vertexSide
        , ref int straightPathCount
        , int maxStraightPath)
    {
#if DEBUG_CROWDSYSTEM_ASSERTS
        Assert.IsTrue(pathSize > 0, "FindStraightPath: The path cannot be empty");
        Assert.IsTrue(path.Length >= pathSize, "FindStraightPath: The array of path polygons must fit at least the size specified");
        Assert.IsTrue(maxStraightPath > 1, "FindStraightPath: At least two corners need to be returned, the start and end");
        Assert.IsTrue(straightPath.Length >= maxStraightPath, "FindStraightPath: The array of returned corners cannot be smaller than the desired maximum corner count");
        Assert.IsTrue(straightPathFlags.Length >= straightPath.Length, "FindStraightPath: The array of returned flags must not be smaller than the array of returned corners");
#endif

        if (!query.IsValid(path[0]))
        {
            straightPath[0] = new NavMeshLocation(); // empty terminator
            return PathQueryStatus.Failure; // | PathQueryStatus.InvalidParam;
        }

        straightPath[0] = query.CreateLocation(startPos, path[0]);

        straightPathFlags[0] = StraightPathFlags.Start;

        var apexIndex = 0;
        var n = 1;

        if (pathSize > 1)
        {
            var startPolyWorldToLocal = query.PolygonWorldToLocalMatrix(path[0]);

            var apex = startPolyWorldToLocal.MultiplyPoint(startPos);
            var left = new Vector3(0, 0, 0); // Vector3.zero accesses a static readonly which does not work in burst yet
            var right = new Vector3(0, 0, 0);
            var leftIndex = -1;
            var rightIndex = -1;

            for (var i = 1; i <= pathSize; ++i)
            {
                var polyWorldToLocal = query.PolygonWorldToLocalMatrix(path[apexIndex]);

                Vector3 vl, vr;
                if (i == pathSize)
                {
                    vl = vr = polyWorldToLocal.MultiplyPoint(endPos);
                }
                else
                {
                    var success = query.GetPortalPoints(path[i - 1], path[i], out vl, out vr);
                    if (!success)
                    {
                        return PathQueryStatus.Failure; // | PathQueryStatus.InvalidParam;
                    }

#if DEBUG_CROWDSYSTEM_ASSERTS
                    Assert.IsTrue(query.IsValid(path[i - 1]));
                    Assert.IsTrue(query.IsValid(path[i]));
#endif

                    vl = polyWorldToLocal.MultiplyPoint(vl);
                    vr = polyWorldToLocal.MultiplyPoint(vr);
                }

                vl = vl - apex;
                vr = vr - apex;

                // Ensure left/right ordering
                if (Perp2D(vl, vr) < 0)
                    Swap(ref vl, ref vr);

                // Terminate funnel by turning
                if (Perp2D(left, vr) < 0)
                {
                    var polyLocalToWorld = query.PolygonLocalToWorldMatrix(path[apexIndex]);
                    var termPos = polyLocalToWorld.MultiplyPoint(apex + left);

                    n = RetracePortals(query, apexIndex, leftIndex, path, n, termPos, ref straightPath, ref straightPathFlags, maxStraightPath);
                    if (vertexSide.Length > 0)
                    {
                        vertexSide[n - 1] = -1;
                    }

                    //Debug.Log("LEFT");

                    if (n == maxStraightPath)
                    {
                        straightPathCount = n;
                        return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
                    }

                    apex = polyWorldToLocal.MultiplyPoint(termPos);
                    left.Set(0, 0, 0);
                    right.Set(0, 0, 0);
                    i = apexIndex = leftIndex;
                    continue;
                }
                if (Perp2D(right, vl) > 0)
                {
                    var polyLocalToWorld = query.PolygonLocalToWorldMatrix(path[apexIndex]);
                    var termPos = polyLocalToWorld.MultiplyPoint(apex + right);

                    n = RetracePortals(query, apexIndex, rightIndex, path, n, termPos, ref straightPath, ref straightPathFlags, maxStraightPath);
                    if (vertexSide.Length > 0)
                    {
                        vertexSide[n - 1] = 1;
                    }

                    //Debug.Log("RIGHT");

                    if (n == maxStraightPath)
                    {
                        straightPathCount = n;
                        return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
                    }

                    apex = polyWorldToLocal.MultiplyPoint(termPos);
                    left.Set(0, 0, 0);
                    right.Set(0, 0, 0);
                    i = apexIndex = rightIndex;
                    continue;
                }

                // Narrow funnel
                if (Perp2D(left, vl) >= 0)
                {
                    left = vl;
                    leftIndex = i;
                }
                if (Perp2D(right, vr) <= 0)
                {
                    right = vr;
                    rightIndex = i;
                }
            }
        }

        // Remove the the next to last if duplicate point - e.g. start and end positions are the same
        // (in which case we have get a single point)
        if (n > 0 && (straightPath[n - 1].position == endPos))
            n--;

        n = RetracePortals(query, apexIndex, pathSize - 1, path, n, endPos, ref straightPath, ref straightPathFlags, maxStraightPath);
        if (vertexSide.Length > 0)
        {
            vertexSide[n - 1] = 0;
        }

        if (n == maxStraightPath)
        {
            straightPathCount = n;
            return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
        }

        // Fix flag for final path point
        straightPathFlags[n - 1] = StraightPathFlags.End;

        straightPathCount = n;
        return PathQueryStatus.Success;
    }
}