//#define DEBUG_INDEXING

using System;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Unity.QuickSearch.Providers
{
    [ExcludeFromPreset, ScriptedImporter(version: SearchDatabase.version, ext: "index")]
    class SearchDatabaseImporter : ScriptedImporter
    {
        private SearchDatabase db { get; set; }

        /// This boolean state is used to delay the importation of indexes
        /// that depends on assets that get imported to late such as prefabs.
        private static bool s_DelayImport = true;

        static SearchDatabaseImporter()
        {
            EditorApplication.delayCall += () => s_DelayImport = false;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var filePath = ctx.assetPath;
            var jsonText = System.IO.File.ReadAllText(filePath);
            var settings = JsonUtility.FromJson<SearchDatabase.Settings>(jsonText);
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            hideFlags |= HideFlags.HideInInspector;

            #if DEBUG_INDEXING
            using (new DebugTimer($"Importing index {fileName}"))
            #endif
            {
                db = ScriptableObject.CreateInstance<SearchDatabase>();
                db.name = fileName;
                db.hideFlags = HideFlags.NotEditable;
                db.settings = settings;
                if (String.IsNullOrEmpty(db.settings.path))
                    db.settings.path = filePath;
                if (String.IsNullOrEmpty(db.settings.name))
                    db.settings.name = fileName;
                db.index = SearchDatabase.CreateIndexer(settings);

                if (!Reimport(filePath))
                {
                    if (ShouldDelayImport())
                    {
                        db.Log("Delayed Import");
                        EditorApplication.delayCall += () => AssetDatabase.ImportAsset(filePath);
                    }
                    else
                    {
                        Build();
                    }
                }

                ctx.AddObjectToAsset(fileName, db);
                ctx.SetMainObject(db);
            }
        }

        private bool ShouldDelayImport()
        {
            if (db.settings.type == nameof(SearchDatabase.IndexType.asset))
                return false;
            return s_DelayImport;
        }

        private bool Reimport(string assetPath)
        {
            if (!SearchDatabase.incrementalIndexCache.TryGetValue(assetPath, out var cachedIndexBytes))
                return false;
            SearchDatabase.incrementalIndexCache.Remove(assetPath);
            db.bytes = cachedIndexBytes;
            return db.index.LoadBytes(cachedIndexBytes, (loaded) =>
                {
                    db.Log($"Reimport.{loaded}");
                    SearchDatabase.SendIndexLoaded(db);
                });
        }

        private void Build()
        {
            try
            {
                db.index.reportProgress += ReportProgress;
                db.index.Build();
                db.bytes = db.index.SaveBytes();
                db.Log("Build");
            }
            finally
            {
                db.index.reportProgress -= ReportProgress;
            }
        }

        private void ReportProgress(int progressId, string description, float progress, bool finished)
        {
            EditorUtility.DisplayProgressBar($"Building {db.name} index...", description, progress);
            if (finished)
                EditorUtility.ClearProgressBar();
        }
    }
}
