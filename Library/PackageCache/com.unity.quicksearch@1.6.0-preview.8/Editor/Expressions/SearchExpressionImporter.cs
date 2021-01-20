using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.QuickSearch
{
    [CustomEditor(typeof(SearchExpressionImporter), editorForChildClasses: false, isFallback = false)]
    class SearchExpressionEditor : Editor
    {
        private SearchExpression m_Expression;
        private ExpressionResultView m_ExpressionResultView;
        private GUIContent m_ExpressionTitle;
        private VisualElement m_ContentViewport;
        private string m_ExpressionName;
        private string m_ExpressionPath;

        private void InitializeExpression()
        {
            if (m_Expression != null)
                return;

            m_ExpressionPath = AssetDatabase.GetAssetPath(target);
            m_ExpressionName = Path.GetFileNameWithoutExtension(m_ExpressionPath);
            m_ExpressionTitle = new GUIContent(m_ExpressionName, Icons.quicksearch, m_ExpressionPath);

            m_Expression = new SearchExpression(SearchSettings.GetContextOptions());
            m_Expression.Load(m_ExpressionPath);
            m_Expression.Evaluate();
        }

        public void OnEnable()
        {
            InitializeExpression();
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (m_ExpressionResultView == null)
                m_ExpressionResultView = new ExpressionResultView(m_Expression);

            m_ExpressionResultView.RegisterCallback<GeometryChangedEvent>(OnSizeChange);
            EditorApplication.delayCall += () => m_ExpressionResultView.style.height = 500;

            return m_ExpressionResultView;
        }

        private void OnSizeChange(GeometryChangedEvent evt)
        {
            if (m_ExpressionResultView == null || m_ExpressionResultView.panel == null || m_ExpressionResultView.panel.visualTree == null)
                return;

            if (m_ContentViewport == null)
            {
                m_ContentViewport = m_ExpressionResultView.panel.visualTree.Query("unity-content-viewport").First();
                if (m_ContentViewport != null)
                    m_ExpressionResultView.panel.visualTree.RegisterCallback<GeometryChangedEvent>(OnSizeChange);
            }
            if (m_ContentViewport != null)
            {
                m_ExpressionResultView.style.height = m_ContentViewport.resolvedStyle.height - 29f;

                var editorsList = m_ExpressionResultView.panel.visualTree.Query(className: "unity-inspector-editors-list").First();
                if (editorsList != null)
                {
                    foreach (var c in editorsList.Children())
                    {
                        if (c.name.StartsWith("TextAssetInspector"))
                        {
                            c.style.height = 0;
                            c.style.display = DisplayStyle.None;
                        }
                    }
                }
            }
        }

        public override bool HasPreviewGUI()
        {
            return false;
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            return Icons.quicksearch;
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        protected override void OnHeaderGUI()
        {
            GUILayout.BeginHorizontal();
            using (new EditorGUIUtility.IconSizeScope(new Vector2(16, 16)))
                GUILayout.Label(m_ExpressionTitle);
            GUILayout.FlexibleSpace();
            if (Utils.isDeveloperBuild)
            {
                if (GUILayout.Button("Refresh"))
                    m_Expression.Evaluate();
            }
            if (GUILayout.Button("Select"))
                EditorGUIUtility.PingObject(target);
            if (GUILayout.Button("Edit"))
                ExpressionBuilder.Open(m_ExpressionPath);
            GUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            // Using UIElements
        }
    }

    [ExcludeFromPreset, ScriptedImporter(version: 2, ext: "qse")]
    class SearchExpressionImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var so = ScriptableObject.CreateInstance<SearchExpressionAsset>();
            so.hideFlags |= HideFlags.HideInInspector;
            ctx.AddObjectToAsset("expression", so, Icons.quicksearch);
            ctx.SetMainObject(so);
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OpenSearchExpression(int instanceID, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            if (!path.EndsWith(".qse", System.StringComparison.OrdinalIgnoreCase))
                return false;

            ExpressionBuilder.Open(path);
            return true;
        }
    }
}
