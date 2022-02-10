

using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;

namespace RPG.Gameplay.Inventory
{
    public struct SlotGUI
    {
        public bool IsFull;
        public int ItemIndex;

        public int2 Scale;

        public int2 Coordinate;

        public float2 Size;

        public Aabb getAabb()
        {
            var collider = GetCollider();
            return collider.Value.CalculateAabb(GetRigidTransform());
        }
        public RigidTransform GetRigidTransform()
        {
            var rigidTransform = new RigidTransform(quaternion.identity, new float3(Coordinate.x * Size.x, Coordinate.y * Size.y, 0f));
            return rigidTransform;
        }
        public BlobAssetReference<Unity.Physics.Collider> GetCollider()
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
    public struct InventoryGUI : IDisposable
    {
        Unity.Physics.Aabb aabb;
        float2 itemSize;
        Inventory inventory;
        SlotGUI[] slots;
        InventoryItem[] items;

        PhysicsWorld world;


        public void Dispose()
        {
            world.Dispose();
        }
        public Aabb GetAabb(int index)
        {
            var body = world.CollisionWorld.Bodies[index];
            return body.CalculateAabb();
        }
        public NativeList<int> ColliderCast(int2 position, int2 size)
        {
            var aabb = new SlotGUI { Coordinate = position, Size = itemSize, Scale = size }.getAabb();
            var hits = new NativeList<int>(Allocator.Temp);
            world.CollisionWorld.OverlapAabb(new OverlapAabbInput { Aabb = aabb, Filter = CollisionFilter.Default }, ref hits);
            hits.Sort();
            return hits;
        }
        public NativeList<int> GetSlots(int index)
        {
            unsafe
            {
                var hits = new NativeList<int>(Allocator.Temp);
                var body = GetAabb(index);
                world.CollisionWorld.OverlapAabb(new OverlapAabbInput { Aabb = body, Filter = CollisionFilter.Default }, ref hits);
                hits.Sort();
                return hits;
            }
        }
        public void Init(Inventory inventory, float2 itemSize)
        {
            slots = new SlotGUI[inventory.Size];
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
                    var collider = slots[index].GetCollider();
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
                if (slot.getAabb().Overlaps(neighbor.getAabb()))
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

            // world.CollisionWorld.BuildBroadphase(ref world, 1.0f, -9.81f * math.up());
            var overlapses = new NativeArray<bool>(slots.Length, Allocator.Temp);
            // var dynamicStream = new NativeStream(slots.Length, Allocator.TempJob);
            // var triggerEventStream = new NativeStream(slots.Length, Allocator.TempJob);
            // var writer = triggerEventStream.AsWriter();
            // var dynamicStreamWriter = dynamicStream.AsWriter();
            // CheckCollision();
            var context = new SimulationContext();
            var input = new SimulationStepInput
            {
                World = world,
                SynchronizeCollisionWorld = false,
                TimeStep = 1f,
                NumSolverIterations = 1,
                SolverStabilizationHeuristicSettings = Solver.StabilizationHeuristicSettings.Default,
                Gravity = -9.81f * math.up()
            };
            context.Reset(input);
            Simulation.StepImmediate(input, ref context);
            foreach (var cEvent in context.CollisionEvents)
            {
                Debug.Log($"body {cEvent.BodyIndexA} collid with {cEvent.BodyIndexB}");
                overlapses[cEvent.BodyIndexB] = true;
            }

            context.Dispose();

            return overlapses;
        }

        private void CheckCollision(ref NativeArray<bool> calculated, ref NativeArray<bool> overlapses, ref NativeArray<Aabb> aabbs, ref NativeArray<bool> traited, int i, int checkNeighborFor)
        {
            if (calculated[i] == false)
            {
                calculated[i] = true;
                // aabbs[i] = slots[i].getAabb(itemSize);
            }
            Debug.Log($"Check Index {i} overlap with neightbor of {checkNeighborFor}");
            var neighborIndexes = GetNeighborsIndex(checkNeighborFor);
            for (int j = 0; j < neighborIndexes.Length; j++)
            {
                var neighborIndex = neighborIndexes[j];
                if (i != neighborIndex && overlapses[neighborIndex] == false && traited[neighborIndex] == false)
                {
                    if (calculated[neighborIndex] == false)
                    {
                        calculated[neighborIndex] = true;
                        // aabbs[neighborIndex] = slots[neighborIndex].getAabb(itemSize);
                    }
                    unsafe
                    {
                        if (slots[i].getAabb().Overlaps(slots[neighborIndex].getAabb()))
                        {
                            Debug.Log($"Index {i} overlap with {neighborIndex}");
                            overlapses[neighborIndex] = true;
                            CheckCollision(ref calculated, ref overlapses, ref aabbs, ref traited, i, neighborIndex);
                        }
                        else
                        {
                            Debug.Log($"Index {i} don't overlap with {neighborIndex}");
                        }

                    }

                }

            }
            traited[i] = true;
        }
        public void ResizeSlot(int2 coordinate, int2 scale)
        {
            var index = inventory.GetIndex(coordinate);
            ResizeSlot(index, scale);
        }
        public void ResizeSlot(int index, int2 scale)
        {
            NativeArray<RigidBody> bodies = world.DynamicBodies;
            var rigid = bodies[index];
            slots[index].Scale = scale;
            rigid.WorldFromBody = slots[index].GetRigidTransform();
            rigid.Collider = slots[index].GetCollider();
            bodies[index] = rigid;
            world.CollisionWorld.UpdateDynamicTree(ref world, 1.0f, -9.81f * math.up());

        }
        public SlotGUI GetSlot(int index)
        {
            return slots[index];
        }
        // public Aabb GetAabb(int index)
        // {
        //     return world.DynamicBodies[index].Collider.Value.CalculateAabb(world.DynamicBodies[index].WorldFromBody);
        // }
        public void AddItem(InventoryItem item)
        {
            var itemAabb = item.GetAabb();
            var point = aabb.ClosestPoint(new float3(item.Position, 0));
            for (int i = 0; i < items.Length; i++)
            {
                var currentItem = items[i];
                var currentItemAabb = currentItem.GetAabb();
                if (currentItemAabb.Overlaps(item.GetAabb()))
                {
                    point = currentItem.GetAabb().Max + math.EPSILON;
                }
            }

        }
    }
    public static class InventoryExtensions
    {
        public static void CreateInventorySlots(EntityManager em, Entity e, Inventory inventory)
        {
            var buffer = em.AddBuffer<InventorySlot>(e);
            buffer.Capacity = inventory.Size;
        }
        public static void MoveItemInInventory(
           Inventory inventory,
           DynamicBuffer<InventorySlot> inventorySlots,
           ref InventoryItem item,
           Entity itemEntity,
           int2 newPosition)
        {
            RemoveItemInInventory(inventory, inventorySlots, item);
            item.Position = newPosition;
            AddItemInInventory(inventory, inventorySlots, item, itemEntity);
        }
        public static void RemoveItemInInventory(
            Inventory inventory,
            DynamicBuffer<InventorySlot> inventorySlots,
            InventoryItem item)
        {
            var itemSlots = inventory.GetSlotForItem(item);
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                inventorySlots[i] = new InventorySlot { Item = Entity.Null };
            }
        }
        public static void AddItemInInventory(
            Inventory inventory,
            DynamicBuffer<InventorySlot> inventorySlots,
            InventoryItem item,
            Entity itemEntity
        )
        {
            var itemSlots = inventory.GetSlotForItem(item);
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                inventorySlots[i] = new InventorySlot { Item = itemEntity };
            }
        }
    }
    public struct InventoryItem
    {
        public int2 Size;
        public float2 Position;

        public Aabb GetAabb()
        {
            var min = new float3(Position.x, Position.y, 0f);
            var max = new float3(Position.x + Size.x, Position.y + Size.y, 0f);
            var aabb = new Aabb { Min = min + math.EPSILON * math.sign(min), Max = max - math.EPSILON * math.sign(max) };
            return aabb;
        }

    }
    public struct InventorySlot : IBufferElementData
    {
        public Entity Item;
    }
    public struct Inventory : IComponentData
    {
        public int Height;
        public int Width;

        public int Size
        {
            get
            {
                return Height * Width;
            }
        }
        public int GetIndex(int x, int y)
        {
            return (y * Width) + x;

        }
        public int GetIndex(int2 coordinate)
        {
            return GetIndex(coordinate.x, coordinate.y);

        }
        public int[] GetSlotForItem(InventoryItem item)
        {
            var slot = new int[Size];
            var slotIndex = 0;
            for (int i = 0; i < item.Size.x; i++)
            {
                for (int j = 0; j < item.Size.y; j++)
                {
                    var index = GetIndex((int)item.Position.x + i, (int)item.Position.y + j);
                    slot[slotIndex] = index;
                    ++slotIndex;
                }

            }
            return slot;
        }
    }
    public struct ItemSlotDescription
    {
        public string GUID;
        public string FriendlyName;
        public string Description;

        public int2 Dimension;

        public Texture2D Texture;
    }
    public class Telegraph : ItemSlot
    {
        public Telegraph()
        {
            AddToClassList("slot-icon-telegraph");
            UnregisterCallback<MouseUpEvent>(OnMouseEventUp);
        }
        override public bool StartDrag()
        {
            Debug.Log("Start Drag Telegraph");
            style.visibility = Visibility.Visible;
            BringToFront();
            return true;
        }
        override public void StopDrag()
        {
            style.visibility = Visibility.Hidden;
            SetPosition(0f);
        }
    }
    public class ItemSlot : VisualElement
    {
        public int _index;

        public int Index { get => _index; set { _index = value; OnChange(); } }

        event Action OnChange;
        float height;
        float width;
        bool isDragging;
        Label indexLabel;
        public ItemSlotDescription ItemSlotDescription { get; private set; }

        protected VisualElement imageBackground;

        public new class UxmlFactory : UxmlFactory<ItemSlot, ItemSlot.UxmlTraits>
        {

        }
        public ItemSlot()
        {

            AddToClassList("slot-icon");
            height = resolvedStyle.height;
            width = resolvedStyle.width;
            imageBackground = new VisualElement();
            imageBackground.AddToClassList("slot-background-container");
            Add(imageBackground);
            RegisterCallback<MouseUpEvent>(OnMouseEventUp);
            //DEBUG TEXT
            var text = new Label();
            text.style.flexGrow = 1;
            OnChange += () =>
            {
                text.text = $"{Index}";
            };
            Add(text);
        }
        public void SetSize(float Width, float Height)
        {
            height = Height;
            width = Width;
        }
        public void SetSize(float2 dimension)
        {
            SetSize(dimension.x, dimension.y);
        }
        protected void OnMouseEventUp(MouseUpEvent mouseUpEvent)
        {
            Debug.Log("On Mouse Event Up e");
            if (!isDragging)
            {
                var rect = layout;
                var mousePosition = new float2(rect.xMin, rect.yMin);
                GetGrid().StartDrag(this, mousePosition);
                mouseUpEvent.StopPropagation();
                return;
            }
        }
        virtual public void StopDrag()
        {
            isDragging = false;
            imageBackground.style.visibility = Visibility.Visible;

        }
        virtual public bool StartDrag()
        {
            if (IsEmpty()) { return false; }
            imageBackground.style.visibility = Visibility.Hidden;
            isDragging = true;
            return true;
        }
        public void SetLocalMousePosition(float2 localMousePosition)
        {
            style.top = localMousePosition.y - layout.height / 2;
            style.left = localMousePosition.x - layout.width / 2;
        }
        public void SetLocalMouseEventPosition(MouseMoveEvent mouseMoveEvent)
        {
            SetLocalMousePosition(mouseMoveEvent.localMousePosition);
        }
        public void SetPosition(float2 vector2)
        {
            style.top = vector2.y;
            style.left = vector2.x;
        }
        public void Resize(int2 dimension)
        {
            style.height = dimension.y * height;
            style.width = dimension.x * width;
        }
        public void SetItem(ItemSlotDescription newItemSlotDescription)
        {
            this.ItemSlotDescription = newItemSlotDescription;
            Resize(newItemSlotDescription.Dimension);
            imageBackground.style.backgroundImage = new StyleBackground(newItemSlotDescription.Texture);
        }
        public void ClearItem()
        {
            this.ItemSlotDescription = default;
            imageBackground.style.backgroundImage = default;
            style.height = height;
            style.width = width;
        }
        public ItemGrid GetGrid()
        {
            return GetFirstAncestorOfType<ItemGrid>();
        }

        public string GetGUID()
        {
            return ItemSlotDescription.GUID;
        }

        public bool IsEmpty()
        {
            return String.IsNullOrEmpty(ItemSlotDescription.GUID) || isDragging;
        }

        public int2 GetCoordinate()
        {
            return new int2((int)(layout.x / width), (int)(layout.y / height));
        }

    }

    public class ItemGrid : VisualElement
    {
        ItemSlot[] items;

        InventoryGUI inventoryGUI;
        public float2 ItemSize = new float2(150, 150);
        bool isDragging;
        ItemSlot originalSlot;

        Telegraph telegraph;

        ItemSlot draggedItem;

        ItemSlot nextSlot;

        Inventory inventory;
        public new class UxmlFactory : UxmlFactory<ItemGrid, ItemGrid.UxmlTraits>
        {

        }
        public ItemGrid()
        {

            AddToClassList("inventory-grid");
            name = "Grid";
            telegraph = new Telegraph();
            telegraph.SetSize(ItemSize);
            Add(telegraph);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }
        void ClearHighlight()
        {
            GetItemSlotsQuery().ForEach((i) =>
            {
                i.RemoveFromClassList("slot-highlight-empty");
                i.RemoveFromClassList("slot-highlight");
            });
        }
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (draggedItem != null)
            {
                PlaceInNewslotIfValid();
                StopDrag(draggedItem);
                ClearHighlight();
                nextSlot = null;
                ReDraw();
            }
        }

        private void PlaceInNewslotIfValid()
        {
            if (nextSlot != null && !IsSameSlot(nextSlot.Index, draggedItem.Index) && !nextSlot.ClassListContains("slot-highlight"))
            {
                RemoveItemSlot(draggedItem.Index);
                SetItemAtSlot(nextSlot.Index, telegraph.ItemSlotDescription);
            }
            else
            {
                // RemoveItemSlot(draggedItem.Index);
                SetItemAtSlot(draggedItem.Index, telegraph.ItemSlotDescription);
            }
        }
        private bool IsSameSlot(int firstSlotIndex, int secondSlotIndex)
        {
            if (firstSlotIndex == secondSlotIndex)
            {
                return true;
            }
            using var firstSlots = inventoryGUI.GetSlots(firstSlotIndex);
            using var secondSlots = inventoryGUI.GetSlots(secondSlotIndex);
            firstSlots.Sort();
            secondSlots.Sort();
            for (int i = 1; i < firstSlots.Length; i++)
            {
                for (int j = 0; j < secondSlots.Length; j++)
                {
                    if (firstSlots[i] == secondSlots[j])
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        void SetItemAtSlot(int index, ItemSlotDescription itemSlotDescription)
        {
            var slots = GetItemSlotsQuery();
            inventoryGUI.ResizeSlot(index, itemSlotDescription.Dimension);
            using var hits = inventoryGUI.GetSlots(index);
            hits.Sort();
            for (var i = 0; i < hits.Length; i++)
            {
                Debug.Log($"item {index} take slot {hits[i]} ");
                var overlapsSlot = slots.AtIndex(hits[i]);
                overlapsSlot.SetItem(itemSlotDescription);
                if (i != 0)
                {
                    inventoryGUI.ResizeSlot(hits[i], 1);
                }

            }
        }
        void ClearSize(ItemSlot slot)
        {
            var slots = inventory.GetSlotForItem(new InventoryItem { Position = slot.GetCoordinate(), Size = slot.ItemSlotDescription.Dimension });
            for (int i = 0; i < slots.Length; i++)
            {
                GetItemSlotAtIndex(slots[i]).style.display = DisplayStyle.Flex;
            }
        }
        void RemoveItemSlot(int index)
        {
            var slots = GetItemSlotsQuery();
            using var hits = inventoryGUI.GetSlots(index);
            for (var i = 0; i < hits.Length; i++)
            {
                var slotFound = slots.AtIndex(hits[i]);
                inventoryGUI.ResizeSlot(hits[i], 1);
                slotFound.ClearItem();
            }

        }
        public bool IsEmpty(int slotIndex)
        {
            var uiSlots = GetItemSlotsQuery();
            var slots = inventoryGUI.GetSlots(slotIndex);
            for (int i = 0; i < slots.Length; i++)
            {
                if (!uiSlots.AtIndex(slots[i]).IsEmpty())
                {
                    return false;
                }
            }
            return true;
        }
        // public void AddToClassList(int slotIndex, string[] classNames)
        // {
        //     var uiSlots = GetItemSlotsQuery();
        //     var slots = inventoryGUI.GetSlots(slotIndex);
        //     for (int i = 0; i < slots.Length; i++)
        //     {
        //         var slot = uiSlots.AtIndex(i)
        //         foreach (var className in classNames)
        //         {
        //             slot.AddToClassList(className);
        //         }
        //     }
        // }
        // public void RemoveClassList(int slotIndex, string[] classNames)
        // {
        //     var uiSlots = GetItemSlotsQuery();
        //     var slots = inventoryGUI.GetSlots(slotIndex);
        //     for (int i = 0; i < slots.Length; i++)
        //     {
        //         var slot = uiSlots.AtIndex(i)
        //         foreach (var className in classNames)
        //         {
        //             slot.RemoveFromClassList(className);
        //         }
        //     }
        // }
        public void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (draggedItem == null) { return; }
            if (nextSlot != null)
            {
                if (IsEmpty(nextSlot.Index))
                {
                    inventoryGUI.ResizeSlot(nextSlot.Index, 1);
                }
                nextSlot.RemoveFromClassList("slot-highlight-empty");
                nextSlot.RemoveFromClassList("slot-highlight");
            }
            // Debug.Log($"Mouse Event From Grid x: {(int)(mouseMoveEvent.localMousePosition.x / ItemSize.x) } y: {(int)(mouseMoveEvent.localMousePosition.y / ItemSize.y)} ");
            var slots = GetItemSlotsQuery();
            var coordinate = new int2
            {
                x = (int)(mouseMoveEvent.localMousePosition.x / ItemSize.x),
                y = (int)(mouseMoveEvent.localMousePosition.y / ItemSize.y),
            };
            telegraph.SetLocalMouseEventPosition(mouseMoveEvent);
            using var result = inventoryGUI.ColliderCast(coordinate, telegraph.ItemSlotDescription.Dimension);
            bool isEmpty = true;
            for (int i = 0; i < result.Length; i++)
            {
                if (!slots.AtIndex(result[i]).IsEmpty())
                {

                    isEmpty = false;
                    break;
                }
            }
            nextSlot = slots.AtIndex(result[0]);
            if (!isEmpty)
            {
                nextSlot.AddToClassList("slot-highlight");
            }
            else
            {
                nextSlot.AddToClassList("slot-highlight-empty");
            }
            // var slotIndex = inventory.GetIndex((int)(mouseMoveEvent.localMousePosition.x / ItemSize.x), (int)(mouseMoveEvent.localMousePosition.y / ItemSize.y));
            // var slotSizeBefore = GetItemSlotAtIndex(slotIndex).ItemSlotDescription.Dimension;
            // // inventoryGUI.ResizeSlot(slotIndex, telegraph.ItemSlotDescription.Dimension);
            // var slots = inventoryGUI.GetSlots(slotIndex);
            // nextSlot = GetItemSlotAtIndex(slots[0]);
            // Debug.Log($"slot index {nextSlot.Index}");
            // if (IsEmpty(nextSlot.Index))
            // {
            //     nextSlot.AddToClassList("slot-highlight-empty");
            // }
            // else
            // {
            //     nextSlot.AddToClassList("slot-highlight");
            // }
            // // inventoryGUI.ResizeSlot(slotIndex, slotSizeBefore);
            // slots.Dispose();

            ReDraw();
        }

        private void ReDraw()
        {
            var slots = GetItemSlotsQuery();
            var overlapses = inventoryGUI.CalculeOverlapse();
            for (int i = 0; i < inventory.Size; i++)
            {
                var uiElementSlot = slots.AtIndex(i);
                var currentSlot = inventoryGUI.GetSlot(i);
                var collider = inventoryGUI.GetAabb(i);
                uiElementSlot.SetPosition(new float2(collider.Min.x, collider.Min.y));
                uiElementSlot.style.position = Position.Absolute;
                uiElementSlot.style.width = collider.Extents.x;
                uiElementSlot.style.height = collider.Extents.y;
                uiElementSlot.style.minWidth = collider.Extents.x;
                uiElementSlot.style.minHeight = collider.Extents.y;
                if (overlapses[i])
                {
                    uiElementSlot.style.display = DisplayStyle.None;
                }
                else
                {
                    uiElementSlot.style.display = DisplayStyle.Flex;
                    uiElementSlot.Resize(currentSlot.Scale);
                }
            }
        }

        public void StartDrag(ItemSlot itemSlot, float2 position)
        {
            if (itemSlot.StartDrag())
            {

                telegraph.SetItem(itemSlot.ItemSlotDescription);
                telegraph.StartDrag();
                telegraph.SetPosition(position);
                RemoveItemSlot(itemSlot.Index);
                draggedItem = itemSlot;

                ReDraw();

            }
        }
        public void StopDrag(ItemSlot itemSlot)
        {
            draggedItem = null;
            itemSlot.StopDrag();
            telegraph.StopDrag();
        }
        public void SetSize(ItemSlot slot, int2 size)
        {
            ClearSize(slot);
            var coordinate = slot.GetCoordinate();
            var slots = inventory.GetSlotForItem(new InventoryItem { Position = coordinate, Size = size });
            for (int i = 1; i < slots.Length; i++)
            {
                GetItemSlotAtIndex(slots[i]).style.display = DisplayStyle.None;
            }
            slot.Resize(size);
        }
        public void AddItem(ItemSlotDescription item)
        {
            var itemSlot = GetItemSlotsQuery();
            var slot = GetItemSlotsQuery().Where((i) => i.IsEmpty()).First();
            if (slot != null)
            {
                SetItemAtSlot(slot.Index, item);
            }
            ReDraw();
        }
        public void InitInventory(Inventory inventory)
        {
            this.style.minWidth = inventory.Width * ItemSize.x;
            this.style.minHeight = inventory.Height * ItemSize.y;
            this.inventory = inventory;
            inventoryGUI = new InventoryGUI();
            inventoryGUI.Init(inventory, ItemSize);
            for (int j = 0; j < inventory.Height; j++)
            {
                for (int i = 0; i < inventory.Width; i++)
                {
                    var itemSlot = new ItemSlot();
                    itemSlot.SetSize(ItemSize);
                    itemSlot.Index = inventory.GetIndex(i, j);
                    Add(itemSlot);
                }
            }

        }
        public UQueryBuilder<ItemSlot> GetItemSlotsQuery()
        {
            return this.Query<ItemSlot>().Where((e) => e != telegraph);
        }
        public ItemSlot GetItemSlotAtIndex(int index)
        {
            return GetItemSlotsQuery().AtIndex(index);
        }
        public void DrawItems(Inventory inventory, NativeArray<InventoryItem> items)
        {

        }
    }
}