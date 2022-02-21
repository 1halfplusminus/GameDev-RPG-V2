

using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Collections;
using System.Collections.Generic;

namespace RPG.Gameplay.Inventory
{

    public class InventoryRootController : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<InventoryRootController, InventoryRootController.UxmlTraits>
        {
            public override string uxmlName => base.uxmlName;

            public override string uxmlNamespace => base.uxmlNamespace;

            public override string uxmlQualifiedName => base.uxmlQualifiedName;

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription => base.uxmlAttributesDescription;

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription => base.uxmlChildElementsDescription;

            public override string substituteForTypeName => base.substituteForTypeName;

            public override string substituteForTypeNamespace => base.substituteForTypeNamespace;

            public override string substituteForTypeQualifiedName => base.substituteForTypeQualifiedName;

            public override bool AcceptsAttributeBag(IUxmlAttributes bag, CreationContext cc)
            {
                return base.AcceptsAttributeBag(bag, cc);
            }

            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                var result = base.Create(bag, cc);
                cc.slotInsertionPoints.TryAdd("Container", result);
                return result;
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string ToString()
            {
                return base.ToString();
            }
        }
    }
    public static class InventoryExtensions
    {

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

    }
    public struct ItemSlotDescription
    {
        public string GUID;
        public string FriendlyName;
        public string Description;

        public int2 Dimension;

        public Texture2D Texture;

        public bool IsEmpty;
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
    public class ItemDetail : VisualElement
    {
        public Label Name;
        public Label Description;

        public Button ActionButton;

        public new class UxmlFactory : UxmlFactory<ItemDetail, ItemDetail.UxmlTraits>
        {

        }

        public ItemDetail()
        {
            RegisterCallback((EventCallback<AttachToPanelEvent>)((cb) =>
            {
                Name = this.Q<Label>("FriendlyName");

                Description = this.Q<Label>("Description");

                ActionButton = this.Q<Button>("btn_Equip");

                Name.text = "";

                Description.text = "";

                ActionButton.clicked += OnActionButton();
            }));

        }

        private Action OnActionButton()
        {
            return () =>
            {
                GetGrid((grid) =>
                {
                    grid.OnActionButton();
                });
            };
        }

        public ItemGrid GetGrid(Action<ItemGrid> callback)
        {
            var root = this.GetFirstAncestorOfType<InventoryRootController>();
            if (root != null)
            {
                var grid = root.Q<ItemGrid>();
                if (grid != null)
                {
                    callback(grid);
                    return grid;
                }
            }
            return null;
        }
        public void ShowItem(ItemSlotDescription itemSlotDescription)
        {
            this.visible = true;
            Name.text = itemSlotDescription.FriendlyName;
            Description.text = itemSlotDescription.Description;
        }
    }
    public class ItemSlot : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ItemSlot, ItemSlot.UxmlTraits>
        {

        }
        public int _index;

        public int Index { get => _index; set { _index = value; OnChange(); } }

        event Action OnChange = () => { };
        float height;
        float width;
        bool isDragging = false;

        public bool IsSelected { get; private set; }
        Label indexLabel;
        public ItemSlotDescription ItemSlotDescription { get; private set; }

        protected VisualElement imageBackground;

        public static void Empty<T>(T value) { }

        public ItemSlot()
        {

            AddToClassList("slot-icon");
            imageBackground = new VisualElement();
            imageBackground.AddToClassList("slot-background-container");
            Add(imageBackground);
            RegisterCallback<MouseUpEvent>(OnMouseEventUp);
            RegisterCallback(OnMouseEnterEvent(this));

        }

        private void ShowDebugText()
        {
            //DEBUG TEXT
            var text = new Label();
            text.style.flexGrow = 1;
            OnChange += () =>
            {
                text.text = $"{Index}";
            };
            Add(text);
        }

        private static EventCallback<MouseEnterEvent> OnMouseEnterEvent(ItemSlot v)
        {
            return (e) =>
            {
                if (!v.IsEmpty())
                {
                    var itemDetail = v.GetFirstAncestorOfType<InventoryRootController>().Q<ItemDetail>();
                    if (itemDetail != null)
                    {

                        itemDetail.ShowItem(v.ItemSlotDescription);
                    }
                }

            };
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
            if (IsEmpty())
            {
                if (mouseUpEvent.clickCount >= 2)
                {
                    GetGrid((grid) =>
                    {
                        grid.OnSelectSlot();
                    });
                }
                return;
            }
            if (!isDragging && mouseUpEvent.clickCount == 1 && !IsSelected)
            {
                AddToClassList("slot-selected");
                IsSelected = true;
                var grid = GetGrid();
                if (grid != null)
                {
                    grid.OnSelectSlot(this);
                }
                return;
            }
            if (!isDragging && (mouseUpEvent.clickCount >= 2 || IsSelected))
            {
                Debug.Log("Double Click pick item");
                var rect = layout;
                var mousePosition = new float2(rect.xMin, rect.yMin);
                GetGrid().StartDrag(this, mousePosition);
                mouseUpEvent.StopPropagation();
                return;
            }
        }

        public void UnSelect()
        {
            RemoveFromClassList("slot-selected");
            IsSelected = false;
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
            var haveItem = !String.IsNullOrEmpty(newItemSlotDescription.GUID);
            if (newItemSlotDescription.IsEmpty || haveItem)
            {
                style.display = DisplayStyle.Flex;
            }
            else
            {
                style.display = DisplayStyle.None;
            }
            if (haveItem)
            {

                ItemSlotDescription = newItemSlotDescription;
                Resize(newItemSlotDescription.Dimension);
                imageBackground.style.backgroundImage = new StyleBackground(newItemSlotDescription.Texture);
            }
            else
            {
                ClearItem();
                ItemSlotDescription = newItemSlotDescription;
            }

        }
        public void ClearItem()
        {
            ItemSlotDescription = default;
            imageBackground.style.backgroundImage = default;
            style.height = height;
            style.width = width;
        }
        public ItemGrid GetGrid(Action<ItemGrid> callback = null)
        {
            var grid = GetFirstAncestorOfType<ItemGrid>();
            if (grid != null)
            {
                if (callback != null)
                    callback(grid);
            }
            return grid;
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

        public InventoryGUI inventoryGUI;
        public float2 ItemSize = new float2(150, 150);
        bool isDragging;
        ItemSlot originalSlot;

        Telegraph telegraph;

        ItemSlot draggedItem;

        ItemSlot nextSlot;

        Inventory inventory;
        bool Selected;
        public (bool MovedThisFrame, int[] OldIndex, int[] NewIndex) ItemMoved;

        public (bool SelectedThisFrame, int Index) ItemSelected;
        public Action<int[], int[]> OnItemMove;

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
            RegisterCallback<DetachFromPanelEvent>((e) =>
            {
                this.inventoryGUI.Dispose();
            });

            RegisterCallback<AttachToPanelEvent>((e) =>
            {
                SetItemDetailVisibility(false);
            });
        }

        public void OnActionButton()
        {
            var selectedSlot = GetItemSlotsQuery().Where((s) => s.IsSelected).First();
            if (selectedSlot != null)
            {
                ItemSelected.SelectedThisFrame = true;
                ItemSelected.Index = selectedSlot.Index;
            }
        }
        public void OnSelectSlot(ItemSlot slot = null)
        {
            GetItemSlotsQuery().Where((i) => (slot == null || i != slot) && i.IsSelected).ForEach((s) =>
            {
                s.UnSelect();

            });
        }

        private void SetItemDetailVisibility(bool visibility)
        {
            var itemDetail = this.GetFirstAncestorOfType<InventoryRootController>().Q<ItemDetail>();
            if (itemDetail != null)
            {
                itemDetail.visible = visibility;
            }
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
            }
        }

        private void PlaceInNewslotIfValid()
        {
            if (nextSlot != null && !IsSameSlot(nextSlot.Index, draggedItem.Index) && !nextSlot.ClassListContains("slot-highlight"))
            {
                ItemMoved.MovedThisFrame = true;
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

        public void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (draggedItem == null) { return; }
            if (nextSlot != null)
            {
                if (nextSlot.ClassListContains("slot-highlight-empty"))
                {
                    inventoryGUI.ResizeSlot(nextSlot.Index, 1);
                    nextSlot.RemoveFromClassList("slot-highlight-empty");
                }
                else
                {
                    nextSlot.RemoveFromClassList("slot-highlight");
                }
            }
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
            if (result.Length > 0)
            {
                nextSlot = slots.AtIndex(result[0]);
                if (!isEmpty)
                {
                    nextSlot.AddToClassList("slot-highlight");
                }
                else
                {
                    ItemMoved.NewIndex = result.ToArray();
                    nextSlot.AddToClassList("slot-highlight-empty");
                }
            }


        }


        public void StartDrag(ItemSlot itemSlot, float2 position)
        {
            if (itemSlot.StartDrag())
            {
                var oldSlots = inventoryGUI.GetSlots(itemSlot.Index);
                telegraph.SetItem(itemSlot.ItemSlotDescription);
                telegraph.StartDrag();
                telegraph.SetPosition(position);
                ItemMoved.OldIndex = oldSlots.ToArray();
                RemoveItemSlot(itemSlot.Index);
                draggedItem = itemSlot;
                oldSlots.Dispose();
            }
        }
        public void StopDrag(ItemSlot itemSlot)
        {
            draggedItem = null;
            itemSlot.StopDrag();
            telegraph.StopDrag();
            OnSelectSlot();
        }

        public void DrawItems(ItemSlotDescription[] items)
        {
            var itemSlot = GetItemSlotsQuery();
            for (int i = 0; i < items.Length; i++)
            {
                itemSlot.AtIndex(i).SetItem(items[i]);
                inventoryGUI.ResizeSlot(i, items[i].Dimension);
            }
            PlaceSlots();
        }
        public void PlaceSlots()
        {
            var slots = GetItemSlotsQuery();
            var overlapses = inventoryGUI.Overlapses;
            for (int i = 0; i < inventory.Size; i++)
            {
                PlaceSlot(slots.AtIndex(i), ref overlapses, i);
            }
        }

        private void PlaceSlot(ItemSlot uiElementSlot, ref NativeArray<bool> overlapses, int i)
        {
            var currentSlot = inventoryGUI.GetSlot(i);
            // var currentSlot = inventoryGUI.GetSlot(i);
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

        public void InitInventory(Inventory inventory)
        {
            style.minWidth = inventory.Width * ItemSize.x;
            style.minHeight = inventory.Height * ItemSize.y;
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

    }
}