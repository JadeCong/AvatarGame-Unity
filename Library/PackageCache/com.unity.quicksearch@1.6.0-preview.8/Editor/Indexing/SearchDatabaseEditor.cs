using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.QuickSearch
{
    [CustomEditor(typeof(SearchDatabase))]
    class SearchDatabaseEditor : Editor
    {
        private SearchDatabase m_DB;
        private SerializedProperty m_Settings;
        private GUIContent m_IndexTitleLabel;

        [SerializeField] private bool m_KeywordsFoldout;
        [SerializeField] private bool m_DocumentsFoldout;
        [SerializeField] private bool m_DependenciesFoldout;

        internal void OnEnable()
        {
            m_DB = (SearchDatabase)target;
            m_Settings = serializedObject.FindProperty("settings");
            m_IndexTitleLabel = new GUIContent($"{m_DB.index?.name ?? m_DB.name} ({EditorUtility.FormatBytes(m_DB.bytes?.Length ?? 0)})");
        }

        public override void OnInspectorGUI()
        {
            if (m_DB.index == null)
                return;

            EditorGUILayout.PropertyField(m_Settings, m_IndexTitleLabel, true);

            EditorGUILayout.IntField($"Indexes", m_DB.index?.indexCount ?? 0);

            var documentTitle = "Documents";
            if (m_DB.index is SceneIndexer objectIndexer)
            {
                var dependencies = objectIndexer.GetDependencies();
                m_DependenciesFoldout = EditorGUILayout.Foldout(m_DependenciesFoldout, $"Documents (Count={dependencies.Count})", true);
                if (m_DependenciesFoldout)
                {
                    foreach (var d in dependencies)
                        EditorGUILayout.LabelField(d);
                }

                documentTitle = "Objects";
            }

            m_DocumentsFoldout = EditorGUILayout.Foldout(m_DocumentsFoldout, $"{documentTitle} (Count={m_DB.index.documentCount})", true);
            if (m_DocumentsFoldout)
            {
                foreach (var documentEntry in m_DB.index.GetDocuments().OrderBy(p=>p))
                    EditorGUILayout.LabelField(documentEntry);
            }

            m_KeywordsFoldout = EditorGUILayout.Foldout(m_KeywordsFoldout, $"Keywords (Count={m_DB.index.keywordCount})", true);
            if (m_KeywordsFoldout)
            {
                foreach (var t in m_DB.index.GetKeywords().OrderBy(p => p))
                    EditorGUILayout.LabelField(t);
            }
        }

        protected override bool ShouldHideOpenButton()
        {
            return true;
        }

        public override bool HasPreviewGUI()
        {
            return false;
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }

        public override bool UseDefaultMargins()
        {
            return true;
        }
    }
}
