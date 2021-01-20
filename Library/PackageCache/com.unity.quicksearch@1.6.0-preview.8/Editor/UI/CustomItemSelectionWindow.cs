using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.QuickSearch
{
    class CustomItemSelectionWindow : DropdownWindow<CustomItemSelectionWindow>
    {
        public class Config
        {
            public bool initialFocus;
            public Action<TreeView, TreeViewItem, object, string, Rect, int, bool, bool> drawRow;
            public Action<TreeView> listInit;
            public float rowWidth;
            public float rowHeight;
            public float windowHeight;
            public IList models;
            public Action<int> elementSelectedHandler;

            public static Config Defaults(IList models, Action<TreeView, TreeViewItem, object, string, Rect, int, bool, bool> drawRow)
            {
                return new Config()
                {
                    models = models,
                    initialFocus = true,
                    drawRow = drawRow
                };
            }
        }

        private Action<int> m_ElementSelectedHandler;
        private CustomItemListView m_ListView;
        private bool m_NeedFocus;

        [UsedImplicitly]
        protected override void OnEnable()
        {
            base.OnEnable();
            m_NeedFocus = true;
        }

        public static void SelectionButton(Rect rect, GUIContent content, GUIStyle style, Func<Config> getConfig)
        {
            DropDownButton(rect, content, style, () =>
            {
                var config = getConfig();
                if (config != null)
                {
                    var window = CreateInstance<CustomItemSelectionWindow>();
                    window.position = new Rect(window.position.x, window.position.y, config.rowWidth, config.windowHeight == 0 ? 200 : config.windowHeight);
                    window.InitWindow(config);
                    return window;
                }

                return null;
            });
        }

        public static void CheckShowWindow(Rect rect, Func<Config> getConfig)
        {
            CheckShowWindow(rect, () =>
            {
                var config = getConfig();
                if (config != null)
                {
                    var window = CreateInstance<CustomItemSelectionWindow>();
                    window.position = new Rect(window.position.x, window.position.y, config.rowWidth, config.windowHeight == 0 ? 200 : config.windowHeight);
                    window.InitWindow(config);
                    return window;
                }

                return null;
            });
        }

        [UsedImplicitly]
        internal void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Event.current.Use();
                    Close();
                    m_ElementSelectedHandler(-1);
                }
            }

            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true));
            m_ListView.OnGUI(rect);
            if (m_NeedFocus && Event.current.type == EventType.Repaint)
            {
                m_NeedFocus = false;
                m_ListView.SetFocusAndEnsureSelectedItem();
            }
        }

        private void InitWindow(Config config)
        {
            m_NeedFocus = config.initialFocus;
            m_ElementSelectedHandler = config.elementSelectedHandler;
            m_ListView = new CustomItemListView(config.models, config.rowHeight, config.drawRow);
            config.listInit?.Invoke(m_ListView);
            m_ListView.elementActivated += OnElementActivated;
        }

        private void OnElementActivated(int indexSelected)
        {
            Close();
            m_ElementSelectedHandler.Invoke(indexSelected);
        }
    }
}