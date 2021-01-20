using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.QuickSearch
{
    class ExpressionBuilder : GraphViewEditorWindow
    {
        private SearchExpression m_Expression;
        private ExpressionGraph m_ExpressionGraph;
        private VisualSplitter m_HorizontalResizer;
        private VisualSplitter m_VerticalResizer;
        private ExpressionInspector m_NodeEditor;
        private ExpressionResultView m_ResultView;

        [SerializeField] private string m_ExpressionPath;
        [SerializeField] private float m_GraphViewSplitterPosition = 0f;
        [SerializeField] private float m_SearchViewSplitterPosition = 150;

        public string expressionPath => m_ExpressionPath;

        public override IEnumerable<GraphView> graphViews { get { yield return m_ExpressionGraph; } }

        [MenuItem("Window/Quick Search/Expression Builder")]
        public static void ShowWindow()
        {
            GetWindow<ExpressionBuilder>();
        }

        public static void Open(string assetPath)
        {
            var existingBuilderWindows = Resources.FindObjectsOfTypeAll<ExpressionBuilder>();
            foreach (var w in existingBuilderWindows)
            {
                if (w.expressionPath.Replace("\\", "/").Equals(assetPath.Replace("\\", "/"), System.StringComparison.OrdinalIgnoreCase))
                {
                    w.Focus();
                    w.Show();
                    return;
                }
            }

            var builder = CreateWindow<ExpressionBuilder>();
            builder.Load(assetPath);
            builder.Show();
        }

        public void OnEnable()
        {
            m_Expression = new SearchExpression(SearchSettings.GetContextOptions());
            m_ExpressionGraph = new ExpressionGraph(m_Expression);

            titleContent = new GUIContent("Expression Builder",  Icons.quicksearch);

            #if UNITY_2020_2_OR_NEWER
            wantsLessLayoutEvents = true;
            #endif

            BuildUI();
            Reload();

            m_ExpressionGraph.nodeChanged += OnNodePropertiesChanged;
            m_ExpressionGraph.graphChanged += OnGraphChanged;
            m_ExpressionGraph.selectionChanged += OnSeletionChanged;
            m_NodeEditor.propertiesChanged += OnNodePropertiesChanged;
            m_NodeEditor.variableAdded += OnNodeVariableAdded;
            m_NodeEditor.variableRemoved += OnNodeVariableRemoved;
            m_NodeEditor.variableRenamed += OnNodeVariableRenamed;
        }

        private void SaveSplitterPosition()
        {
            m_SearchViewSplitterPosition = m_NodeEditor?.style.height.value.value ?? m_SearchViewSplitterPosition;
            m_GraphViewSplitterPosition = m_ExpressionGraph?.style.width.value.value ?? m_GraphViewSplitterPosition;
        }

        public void OnDisable()
        {
            SaveSplitterPosition();

            m_ExpressionGraph.nodeChanged -= OnNodePropertiesChanged;
            m_ExpressionGraph.graphChanged -= OnGraphChanged;
            m_ExpressionGraph.selectionChanged -= OnSeletionChanged;
            m_ExpressionGraph.Dispose();
            m_ExpressionGraph = null;

            m_NodeEditor.propertiesChanged -= OnNodePropertiesChanged;
            m_NodeEditor.variableAdded -= OnNodeVariableAdded;
            m_NodeEditor.variableRemoved -= OnNodeVariableRemoved;
            m_NodeEditor.variableRenamed -= OnNodeVariableRenamed;
            m_NodeEditor.Dispose();
            m_NodeEditor = null;

            m_Expression.Dispose();
            m_Expression = null;
        }

        private void OnGraphChanged()
        {
            UpdateSelection(m_ExpressionGraph.selection);
        }

        private void OnNodePropertiesChanged(SearchExpressionNode ex)
        {
            m_ExpressionGraph.UpdateNode(ex);
        }

        private void OnNodeVariableRenamed(SearchExpressionNode ex, string oldVariableName, string newVariableName)
        {
            m_ExpressionGraph.RenameNodeVariable(ex, oldVariableName, newVariableName);
        }

        private void OnNodeVariableRemoved(SearchExpressionNode ex, string varName)
        {
            m_ExpressionGraph.RemoveNodeVariable(ex, varName);
        }

        private void OnNodeVariableAdded(SearchExpressionNode ex, string varName)
        {
            m_ExpressionGraph.AddNodeVariable(ex, varName);
        }

        private void BuildUI()
        {
            BuildToolbox(m_ExpressionGraph);

            m_NodeEditor = new ExpressionInspector();
            m_ResultView = new ExpressionResultView(m_Expression);

            m_VerticalResizer = new VisualSplitter(m_NodeEditor, m_ResultView, FlexDirection.Column);
            m_HorizontalResizer = new VisualSplitter(m_ExpressionGraph, m_VerticalResizer, FlexDirection.Row);

            rootVisualElement.Add(m_HorizontalResizer);
            rootVisualElement.style.flexDirection = FlexDirection.Row;
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnSizeChange);

            EditorApplication.delayCall += () =>
            {
                if (m_GraphViewSplitterPosition == 0f)
                    m_GraphViewSplitterPosition = position.width * 0.7f;

                m_NodeEditor.style.height = m_SearchViewSplitterPosition;
                m_ExpressionGraph.style.width = m_GraphViewSplitterPosition;
            };
        }

        private void OnSizeChange(GeometryChangedEvent evt)
        {
            if (evt.oldRect.width == 0)
                return;
            var widthDiff = evt.oldRect.width - evt.newRect.width;
            m_ExpressionGraph.style.width = m_ExpressionGraph.style.width.value.value - widthDiff;
        }

        private void UpdateSelection(IList<ISelectable> selection)
        {
            var selectedNode = selection.Select(s => s as Node).Where(s => s != null).LastOrDefault();
            if (selectedNode != null)
                Evaluate(selectedNode);

            if (selectedNode != null && selectedNode.userData is SearchExpressionNode ex)
            {
                m_NodeEditor.SetSelection(ex);
                if (ex.type == ExpressionType.Select)
                    m_ResultView.itemIconSize = 0f;
                else
                    m_ResultView.itemIconSize = 1f;
            }
            else
                m_NodeEditor.ClearSelection();

            m_NodeEditor.MarkDirtyRepaint();
            m_ResultView.MarkDirtyRepaint();
        }

        private void OnSeletionChanged(IList<ISelectable> selection)
        {
            UpdateSelection(selection);
        }

        private VisualElement FlexibleSpace()
        {
            var space = new VisualElement();
            space.style.flexGrow = 1;
            return space;
        }

        private void BuildToolbox(VisualElement container)
        {
            var toolbox = new VisualElement();
            toolbox.style.flexDirection = FlexDirection.Row;

            toolbox.Add(new Button(() => m_ExpressionGraph.AddNode(ExpressionType.Provider)) { text = "Provider" });
            toolbox.Add(new Button(() => m_ExpressionGraph.AddNode(ExpressionType.Value)) { text = "Value" });
            toolbox.Add(new Button(() => m_ExpressionGraph.AddNode(ExpressionType.Search)) { text = "Search" });
            toolbox.Add(new Button(() => m_ExpressionGraph.AddNode(ExpressionType.Select)) { text = "Select" });
            toolbox.Add(new Button(() => m_ExpressionGraph.AddNode(ExpressionType.Union)) { text = "Union" });
            toolbox.Add(new Button(() => m_ExpressionGraph.AddNode(ExpressionType.Intersect)) { text = "Intersect" });
            toolbox.Add(new Button(() => m_ExpressionGraph.AddNode(ExpressionType.Except)) { text = "Except" });
            toolbox.Add(new Button(() => m_ExpressionGraph.AddNode(ExpressionType.Expression)) { text = "Expression" });

            toolbox.Add(FlexibleSpace());

            if (Utils.isDeveloperBuild)
            {
                toolbox.Add(new Button(ExportSJSON) { text = "Export", tooltip = "Copy expression as SJSON in the clipboard"});
                toolbox.Add(new Button(Reload) { text = "Reload" });
            }
            toolbox.Add(new Button(Save) { text = "Save" });

            container.Add(toolbox);
        }

        private void ExportSJSON()
        {
            EditorGUIUtility.systemCopyBuffer = SJSON.Encode(m_Expression.Export());
        }

        public void Load(string assetPath)
        {
            if (!File.Exists(assetPath))
                return;
            m_ExpressionPath = assetPath;
            Reload();
        }

        private void Evaluate(Node node)
        {
            if (node.userData is SearchExpressionNode ex)
            {
                switch (ex.type)
                {
                    case ExpressionType.Results:
                        m_Expression.Evaluate();
                        break;
                    case ExpressionType.Search:
                    case ExpressionType.Select:
                    case ExpressionType.Union:
                    case ExpressionType.Intersect:
                    case ExpressionType.Except:
                    case ExpressionType.Expression:
                        m_Expression.EvaluateNode(ex);
                        break;
                    case ExpressionType.Value:
                    case ExpressionType.Provider:
                        ex = m_Expression.FromSource(ex);
                        if (ex != null)
                            m_Expression.EvaluateNode(ex);
                        break;

                    default:
                        throw new ExpressionException($"Evaluation of {ex.id} of type {ex.type} is not supported");
                }
            }
        }

        private bool IsCurrentExpressionPathValid()
        {
            return m_ExpressionPath != null && File.Exists(m_ExpressionPath);
        }

        private void Reload()
        {
            if (!IsCurrentExpressionPathValid())
                return;

            m_Expression.Reset();
            m_Expression.Load(m_ExpressionPath);
            m_ExpressionGraph.Reload();

            UpdateDocumentInfo();
        }

        private void UpdateDocumentInfo()
        {
            titleContent = new GUIContent(Path.GetFileName(m_ExpressionPath), Icons.quicksearch, m_ExpressionPath);
        }

        private void Save()
        {
            if (String.IsNullOrEmpty(m_ExpressionPath))
                m_ExpressionPath = EditorUtility.SaveFilePanel("Save search expression...", null, "expression", "qse").Replace("\\", "/");
            if (!String.IsNullOrEmpty(m_ExpressionPath))
            {
                m_Expression.Save(m_ExpressionPath);
                UpdateDocumentInfo();

                if (m_ExpressionPath.StartsWith(Application.dataPath.Replace("\\", "/")))
                {
                    var relativepath = "Assets" + m_ExpressionPath.Substring(Application.dataPath.Replace("\\", "/").Length);
                    AssetDatabase.ImportAsset(relativepath);
                }
            }
        }
    }
}
