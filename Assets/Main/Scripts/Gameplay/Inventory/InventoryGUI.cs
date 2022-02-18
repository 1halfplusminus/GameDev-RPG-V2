using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace RPG.Gameplay.Inventory
{
    public struct SlotGUI : IComponentData
    {
        public int2 Scale;

        public int2 Coordinate;

        public float2 Size;

        public Aabb CalculateAabb()
        {
            var collider = CalculateCollider();
            return collider.Value.CalculateAabb(GetRigidTransform());
        }
        public RigidTransform GetRigidTransform()
        {
            var rigidTransform = new RigidTransform(quaternion.identity, new float3(Coordinate.x * Size.x, Coordinate.y * Size.y, 0f));
            return rigidTransform;
        }
        public BlobAssetReference<Unity.Physics.Collider> CalculateCollider()
        {

            var size = new float3(Size.x, Size.y, 1f);
            var scaledSize = size * new float3(Scale.x - 0.1f, Scale.y - 0.1f, 1f);
            var boxGeometry = new BoxGeometry { Center = scaledSize / 2.0f, Orientation = quaternion.identity, Size = scaledSize };
            var mat = new Unity.Physics.Material();
            mat.CollisionResponse = CollisionResponsePolicy.CollideRaiseCollisionEvents;
            var boxBlob = Unity.Physics.BoxCollider.Create(boxGeometry, CollisionFilter.Default, mat);
            return boxBlob;
        }
    }
    public struct InventoryGUI : System.IDisposable
    {
        Unity.Physics.Aabb aabb;
        float2 itemSize;
        Inventory inventory;
        NativeArray<SlotGUI> slots;

        PhysicsWorld world;

        Simulation simulation;
        JobHandle handle;
        public NativeArray<bool> Overlapses;

        public void Dispose()
        {
            world.Dispose();
            if (Overlapses.IsCreated)
            {
                Overlapses.Dispose();
            }
            if (slots.IsCreated)
            {
                slots.Dispose();
            }
            if (simulation != null)
            {
                simulation.Dispose();
            }

        }
        public static InventoryGUI Build(Inventory inventory, NativeArray<InventoryItem> items)
        {
            var inventoryGUI = new InventoryGUI { };
            inventoryGUI.Init(inventory, 1f);
            for (int i = 0; i < inventory.Size; i++)
            {
                var item = InventoryItem.Empty;
                item.Index = i;
                item.ItemDefinition = Entity.Null;
                item.IsEmpty = true;
                items[i] = item;
            }
            return inventoryGUI;
        }
        public bool Insert(int i, InventoryItem item, NativeArray<InventoryItem> items)
        {
            if (!items[i].IsEmpty || i >= slots.Length)
            {
                return false;
            }
            var allEmpty = true;
            var takenSlots = ColliderCast(slots[i].Coordinate, item.ItemDefinitionAsset.Value.Dimension);
            for (int j = 0; j < takenSlots.Length; j++)
            {
                var slot = takenSlots[j];
                var inventoryItem = items[slot];
                if (!inventoryItem.IsEmpty)
                {
                    allEmpty = false;
                }

            }
            if (allEmpty)
            {
                for (int j = 0; j < takenSlots.Length; j++)
                {
                    var slot = takenSlots[j];
                    var inventoryItem = items[slot];
                    var currentItem = j == 0 ? item : items[slot];
                    currentItem.IsEmpty = false;
                    currentItem.Index = slot;
                    items[slot] = currentItem;
                }
                Debug.Log($"Insert at slot {takenSlots[0]} item {item.ItemDefinitionAsset.Value.GUID.ToString()}");
                ResizeSlot(takenSlots[0], item.ItemDefinitionAsset.Value.Dimension);
                takenSlots.Dispose();
                return true;
            }
            takenSlots.Dispose();
            return false;
        }
        public bool Add(InventoryItem item, NativeArray<InventoryItem> items)
        {
            bool inserted = false;
            for (int i = 0; i < items.Length; i++)
            {
                inserted = Insert(i, item, items);
                if (inserted)
                {
                    break;
                }
            }
            return inserted;
        }
        public Aabb GetAabb(int index)
        {
            var body = world.CollisionWorld.Bodies[index];
            return body.CalculateAabb();
        }
        public NativeList<int> ColliderCast(int2 position, int2 size)
        {
            UpdateDynamicTree();
            var aabb = new SlotGUI { Coordinate = position, Size = itemSize, Scale = size }.CalculateAabb();
            var hits = new NativeList<int>(Allocator.Temp);
            world.CollisionWorld.OverlapAabb(new OverlapAabbInput { Aabb = aabb, Filter = CollisionFilter.Default }, ref hits);
            hits.Sort();

            return hits;
        }
        public NativeList<int> GetSlots(int index)
        {
            unsafe
            {
                UpdateDynamicTree();
                var hits = new NativeList<int>(Allocator.Temp);
                var body = GetAabb(index);
                world.CollisionWorld.OverlapAabb(new OverlapAabbInput { Aabb = body, Filter = CollisionFilter.Default }, ref hits);
                hits.Sort();
                if (hits.IsEmpty)
                {
                    hits.Resize(1, NativeArrayOptions.UninitializedMemory);
                    hits[0] = index;
                }
                return hits;
            }
        }
        SimulationStepInput CreateStepInput()
        {
            return new SimulationStepInput
            {
                World = world,
                SynchronizeCollisionWorld = false,
                TimeStep = 1f,
                NumSolverIterations = 1,
                SolverStabilizationHeuristicSettings = Solver.StabilizationHeuristicSettings.Default,
                Gravity = -9.81f * math.up()
            };
        }
        void InitSimulation()
        {

            simulation = new Simulation();
            // // var context = new SimulationContext();
            // var input = ;
            // context.Reset(input);

        }
        public void Init(Inventory inventory, float2 itemSize)
        {
            Overlapses = new NativeArray<bool>(inventory.Size, Allocator.Persistent);
            slots = new NativeArray<SlotGUI>(inventory.Size, Allocator.Persistent);
            aabb = new Aabb { Min = float3.zero, Max = new float3(inventory.Width * itemSize.x, inventory.Height * itemSize.y, 0) };
            this.itemSize = itemSize;
            this.inventory = inventory;
            world = new PhysicsWorld(0, inventory.Size, 0);
            NativeArray<RigidBody> bodies = world.DynamicBodies;
            var MotionVelocities = world.MotionVelocities;

            var MotionDatas = world.MotionDatas;
            var defaultPhysicsDamping = new PhysicsDamping
            {
                Linear = 0.0f,
                Angular = 0.0f,
            };

            for (int i = 0; i < inventory.Width; i++)
            {
                for (int j = 0; j < inventory.Height; j++)
                {
                    var index = inventory.GetIndex(i, j);
                    slots[index] = new SlotGUI { Coordinate = new int2(i, j), Scale = new int2(1, 1), Size = itemSize };
                    var collider = slots[index].CalculateCollider();
                    var RigidTransform = slots[index].GetRigidTransform();
                    var defaultPhysicsMass = PhysicsMass.CreateKinematic(collider.Value.MassProperties);
                    PhysicsMass mass = defaultPhysicsMass;
                    PhysicsDamping damping = defaultPhysicsDamping;
                    MotionDatas[index] = new MotionData
                    {
                        WorldFromMotion = new RigidTransform(
                                math.mul(quaternion.identity, mass.InertiaOrientation),
                                math.rotate(quaternion.identity, mass.CenterOfMass) + RigidTransform.pos
                                ),
                        BodyFromMotion = new RigidTransform(mass.InertiaOrientation, mass.CenterOfMass),
                        LinearDamping = damping.Linear,
                        AngularDamping = damping.Angular
                    };
                    MotionVelocities[index] = new MotionVelocity
                    {
                        LinearVelocity = float3.zero,
                        AngularVelocity = float3.zero,
                        InverseInertia = defaultPhysicsMass.InverseInertia,
                        InverseMass = defaultPhysicsMass.InverseMass,
                        AngularExpansionFactor = defaultPhysicsMass.AngularExpansionFactor,
                        GravityFactor = 0f
                    };
                    bodies[index] = new RigidBody
                    {
                        WorldFromBody = RigidTransform,
                        Collider = collider,
                        Entity = Entity.Null,
                        CustomTags = 0
                    };
                }
            }
            InitSimulation();
            CheckCollision();
        }
        public void CheckCollision()
        {
            world.CollisionWorld.BuildBroadphase(ref world, 1.0f, -9.81f * math.up());
        }
        public NativeList<int> GetNeighborsIndex(int2 coordinate)
        {
            return GetNeighborsIndex(inventory.GetIndex(coordinate));
        }
        public NativeList<int> GetNeighborsIndex(int index)
        {
            var slot = slots[index];
            var list = new NativeList<int>(Allocator.Temp);
            var neighborCoordinates = new int2[] {
                slot.Coordinate + new int2(1,0),
                slot.Coordinate + new int2(0,1),
                slot.Coordinate + new int2(1,1),
                slot.Coordinate + new int2(-1,0),
                slot.Coordinate + new int2(0,-1),
                slot.Coordinate + new int2(-1,-1),
                slot.Coordinate + new int2(-1,1),
                slot.Coordinate + new int2(1,-1),
            };
            for (int i = 0; i < neighborCoordinates.Length; i++)
            {
                var neighborCoordinate = neighborCoordinates[i];
                if (neighborCoordinate.x >= 0 && neighborCoordinate.y >= 0)
                {
                    var neighborIndex = inventory.GetIndex(neighborCoordinate);
                    if (neighborIndex < slots.Length && neighborIndex >= 0)
                    {
                        list.Add(neighborIndex);
                    }
                }
            }
            return list;
        }
        public bool IsVisible(int index)
        {
            var slot = slots[index];
            var neighborIndexes = GetNeighborsIndex(index);
            for (int i = 0; i < neighborIndexes.Length; i++)
            {
                var neightborIndex = neighborIndexes[i];
                var neighbor = slots[neightborIndex];
                if (slot.CalculateAabb().Overlaps(neighbor.CalculateAabb()))
                {
                    Debug.Log($"Index {index} overlap with {neightborIndex}");
                    return false;
                }
            }
            neighborIndexes.Dispose();
            return true;
        }
        public NativeArray<bool> CalculeOverlapse()
        {
            ResetOverlapses();
            world.CollisionWorld.UpdateDynamicTree(ref world, 1.0f, -9.81f * math.up());
            if (world.Bodies.Length > 0)
            {
                simulation.Step(CreateStepInput());
            }


            foreach (var cEvent in simulation.CollisionEvents)
            {
                Debug.Log($"body {cEvent.BodyIndexA} collid with {cEvent.BodyIndexB}");
                Overlapses[cEvent.BodyIndexB] = true;
            }

            return Overlapses;
        }
        public JobHandle ScheduleCalculeOverlapse()
        {
            ResetOverlapses();
            UpdateDynamicTree();
            SimulationCallbacks callbacks = new SimulationCallbacks();
            if (simulation != null)
            {
                simulation.Dispose();
            }
            InitSimulation();
            var _simulation = simulation;
            var _overlaps = Overlapses;
            var _world = world;

            callbacks.Enqueue(SimulationCallbacks.Phase.PostSolveJacobians, (ref ISimulation iSimulation, ref PhysicsWorld world, JobHandle handle) =>
            {
                handle.Complete();
                foreach (var cEvent in _simulation.CollisionEvents)
                {
                    Debug.Log($"body {cEvent.BodyIndexA} collid with {cEvent.BodyIndexB}");
                    _overlaps[cEvent.BodyIndexB] = true;
                }
                return handle;
            }, handle);

            var simulationJobHandle = simulation.ScheduleStepJobs(CreateStepInput(), callbacks, handle, false);
            handle = simulation.FinalJobHandle;

            return handle;
        }

        private void ResetOverlapses()
        {
            for (int i = Overlapses.Length - 1; i >= 0; i--)
            {
                Overlapses[i] = false;
            }
        }

        public void ResizeSlot(int2 coordinate, int2 scale)
        {
            var index = inventory.GetIndex(coordinate);
            ResizeSlot(index, scale);
        }
        public void ResizeSlot(int index, int2 scale)
        {
            NativeArray<RigidBody> bodies = world.DynamicBodies;
            var slot = slots[index];
            var rigid = bodies[index];
            slot.Scale = scale;
            slots[index] = slot;
            rigid.WorldFromBody = slot.GetRigidTransform();
            rigid.Collider = slot.CalculateCollider();
            bodies[index] = rigid;

        }
        void UpdateDynamicTree()
        {
            handle.Complete();
            world.CollisionWorld.UpdateDynamicTree(ref world, 1.0f, -9.81f * math.up());
        }
        public SlotGUI GetSlot(int index)
        {
            return slots[index];
        }

    }
}