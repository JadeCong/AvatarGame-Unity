using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.QuickSearch
{
    /// <summary>
    /// A view able to display a <see cref="ISearchList"/> of <see cref="SearchItem"/>s.
    /// </summary>
    interface IResultView
    {
        /// <summary>Items to be displayed.</summary>
        ISearchList items { get; }
        /// <summary>Item size in pixels.</summary>
        float itemSize { get; }
        /// <summary>Scroll position of the content area of the view.</summary>
        Vector2 scrollPosition { get; }
        /// <summary>Search view that contains the text area where a query is entered.</summary>
        ISearchView searchView { get; }
        /// <summary>Rect of the result view.</summary>
        Rect rect { get; }
        /// <summary>
        /// Draw the items in a specified rect area specifying which items are selected.
        /// </summary>
        /// <param name="rect">Rect of the drawing area.</param>
        /// <param name="selection">Indexes of items to draw as selected</param>
        void Draw(Rect rect, ICollection<int> selection);
        /// <summary>
        /// Draw the items specifying which items are selected.
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="sliderPos"></param>
        /// <param name="focusSelectedItem"></param>
        void Draw(ICollection<int> selection, float sliderPos, ref bool focusSelectedItem);
        /// <summary>
        ///  Get how many items can be shown in the display area depending on the <see cref="IResultView.itemSize"/>
        /// </summary>
        /// <returns>Returns the number of visible items.</returns>
        int GetDisplayItemCount();
    }

    abstract class ResultView : IResultView
    {
        protected Vector2 m_ScrollPosition;
        protected bool m_PrepareDrag;
        protected Vector3 m_DragStartPosition;
        protected int m_MouseDownItemIndex;
        protected double m_ClickTime = 0;
        protected Rect m_DrawItemsRect = Rect.zero;

        public ResultView(ISearchView hostView)
        {
            searchView = hostView;
        }

        public ISearchList items => searchView.results;
        public float itemSize => searchView.itemIconSize;
        public Vector2 scrollPosition { get => m_ScrollPosition; set => m_ScrollPosition = value; }
        public ISearchView searchView { get; private set; }
        public SearchContext context => searchView.context;
        public Rect rect => m_DrawItemsRect;

        public abstract int GetDisplayItemCount();
        protected abstract void Draw(Rect rect, ICollection<int> selection, ref bool focusSelectedItem);

        public void Draw(Rect rect, ICollection<int> selection)
        {
            bool _dummy = false;
            if (Event.current.type == EventType.Repaint)
                m_DrawItemsRect = rect;
            Draw(rect, selection, ref _dummy);
        }

        public void Draw(ICollection<int> selection, float sliderPos, ref bool focusSelectedItem)
        {
            GUILayout.Box(String.Empty, GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.Width(sliderPos-2f));
            if (Event.current.type == EventType.Repaint)
                m_DrawItemsRect = GUILayoutUtility.GetLastRect();

            Draw(m_DrawItemsRect, selection, ref focusSelectedItem);
        }

        protected bool IsDragClicked(Event evt)
        {
            if (evt.type != EventType.DragExited)
                return false;
            return IsDragClicked(evt.mousePosition);
        }

        protected bool IsDragClicked(Vector2 mousePosition)
        {
            var dragDistance = Vector2.Distance(m_DragStartPosition, mousePosition);
            #if false // Used to debug miss double clicks.
            Debug.Log($"Drag distance: {dragDistance}");
            #endif
            return dragDistance < 15;
        }

        protected void HandleMouseDown(int clickedItemIndex)
        {
            m_PrepareDrag = true;
            m_MouseDownItemIndex = clickedItemIndex;
            m_DragStartPosition = Event.current.mousePosition;
        }

        protected void HandleMouseUp(int clickedItemIndex, int itemTotalCount)
        {
            var evt = Event.current;
            var mouseDownItemIndex = m_MouseDownItemIndex;
            m_MouseDownItemIndex = -1;

            if (SearchField.IsAutoCompleteHovered(evt.mousePosition))
                return;

            if (clickedItemIndex >= 0 && clickedItemIndex < itemTotalCount)
            {
                if (evt.button == 0 && mouseDownItemIndex == clickedItemIndex)
                {
                    var ctrl = evt.control || evt.command;
                    var now = EditorApplication.timeSinceStartup;
                    if (searchView.multiselect && evt.modifiers.HasFlag(EventModifiers.Shift))
                    {
                        int min = searchView.selection.MinIndex();
                        int max = searchView.selection.MaxIndex();

                        if (clickedItemIndex > min)
                        {
                            if (ctrl && clickedItemIndex > max)
                            {
                                var r = 0;
                                var range = new int[clickedItemIndex - max];
                                for (int i = max+1; i <= clickedItemIndex; ++i)
                                    range[r++] = i;
                                searchView.AddSelection(range);
                            }
                            else
                            {
                                var r = 0;
                                var range = new int[clickedItemIndex - min + 1];
                                for (int i = min; i <= clickedItemIndex; ++i)
                                    range[r++] = i;
                                searchView.SetSelection(range);
                            }
                        }
                        else if (clickedItemIndex < max)
                        {
                            if (ctrl && clickedItemIndex < min)
                            {
                                var r = 0;
                                var range = new int[min - clickedItemIndex];
                                for (int i = min-1; i >= clickedItemIndex; --i)
                                    range[r++] = i;
                                searchView.AddSelection(range);
                            }
                            else
                            {
                                var r = 0;
                                var range = new int[max - clickedItemIndex + 1];
                                for (int i = max; i >= clickedItemIndex; --i)
                                    range[r++] = i;
                                searchView.SetSelection(range);
                            }
                        }
                    }
                    else if (searchView.multiselect && ctrl)
                    {
                        searchView.AddSelection(clickedItemIndex);
                    }
                    else
                        searchView.SetSelection(clickedItemIndex);

                    if ((now - m_ClickTime) < 0.2)
                    {
                        var item = items.ElementAt(clickedItemIndex);
                        if (item.provider.actions.Count > 0)
                            searchView.ExecuteAction(item.provider.actions[0], new []{item});
                        GUIUtility.ExitGUI();
                    }
                    SearchField.Focus();
                    evt.Use();
                    m_ClickTime = now;
                }
                else if (evt.button == 1)
                {
                    var item = items.ElementAt(clickedItemIndex);
                    var contextRect = new Rect(evt.mousePosition, new Vector2(1, 1));
                    if (item.provider.openContextual == null || !item.provider.openContextual(searchView.selection, contextRect))
                    {
                        if (searchView.selection.Count <= 1)
                            searchView.ShowItemContextualMenu(item, contextRect);
                    }
                }
            }

            // Reset drag content
            m_PrepareDrag = false;
            DragAndDrop.PrepareStartDrag();
        }

        protected void HandleMouseDrag(int dragIndex, int itemTotalCount)
        {
            if (IsDragClicked(Event.current.mousePosition))
                return;
            if (dragIndex >= 0 && dragIndex < itemTotalCount)
            {
                var item = items.ElementAt(dragIndex);
                if (item.provider?.startDrag != null)
                {
                    item.provider.startDrag(item, searchView.context);
                    m_PrepareDrag = false;

                    Event.current.Use();
                    #if UNITY_EDITOR_OSX
                    searchView.Close();
                    GUIUtility.ExitGUI();
                    #endif
                }
            }
        }
    }
}