//#define DEBUG_INDEXING

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.QuickSearch
{
    class AssetIndexer : ObjectIndexer
    {
        public AssetIndexer(SearchDatabase.Settings settings)
            : base("assets", settings)
        {
            getEntryComponentsHandler = GetEntryComponents;
        }

        public IEnumerable<string> GetEntryComponents(string path, int index)
        {
            return SearchUtils.SplitFileEntryComponents(path, entrySeparators);
        }

        protected override System.Collections.IEnumerator BuildAsync(int progressId, object userData = null)
        {
            var paths = GetDependencies();
            var pathIndex = 0;
            var pathCount = (float)paths.Count;

            Start(clear: true);

            EditorApplication.LockReloadAssemblies();
            lock (this)
            {
                foreach (var path in paths)
                {
                    var progressReport = pathIndex++ / pathCount;
                    ReportProgress(progressId, path, progressReport, false);
                    IndexDocument(path, false);
                    yield return null;
                }
            }
            EditorApplication.UnlockReloadAssemblies();

            Finish((bytes) => {}, null, false);
            while (!IsReady())
                yield return null;

            ReportProgress(progressId, $"Indexing Completed (Documents: {documentCount}, Indexes: {indexCount:n0})", 1f, true);
            yield return null;
        }

        public override IEnumerable<string> GetRoots()
        {
            if (settings.roots == null || settings.roots.Length == 0)
                return new string[] { Path.GetDirectoryName(settings.path).Replace("\\", "/") };
            return settings.roots.Where(r => Directory.Exists(r));
        }

        public override List<string> GetDependencies()
        {
            string[] roots = GetRoots().ToArray();
            return AssetDatabase.FindAssets(String.Empty, roots)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct().Where(path => !SkipEntry(path)).ToList();
        }

        public override Hash128 GetDocumentHash(string path)
        {
            return AssetDatabase.GetAssetDependencyHash(path);
        }

        public override void IndexDocument(string path, bool checkIfDocumentExists)
        {
            var documentIndex = AddDocument(path, checkIfDocumentExists);
            AddDocumentHash(path, GetDocumentHash(path));
            if (documentIndex < 0)
                return;

            IndexWordComponents(path, documentIndex, path);

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                IndexWord(path, fileName, documentIndex, fileName.Length, true);

                IndexWord(path, path, documentIndex, path.Length, exact: true);
                IndexProperty(path, "id", path, documentIndex, saveKeyword: false, exact: true);

                if (path.StartsWith("Packages/", StringComparison.Ordinal))
                    IndexProperty(path, "a", "packages", documentIndex, saveKeyword: true, exact: true);
                else
                    IndexProperty(path, "a", "assets", documentIndex, saveKeyword: true, exact: true);

                if (!String.IsNullOrEmpty(name))
                    IndexProperty(path, "a", name, documentIndex, saveKeyword: true, exact: true);

                if (settings.options.fstats)
                {
                    var fi = new FileInfo(path);
                    if (fi.Exists)
                    {
                        IndexNumber(path, "size", (double)fi.Length, documentIndex);
                        IndexProperty(path, "ext", fi.Extension.Replace(".", "").ToLowerInvariant(), documentIndex, saveKeyword: false);
                        IndexNumber(path, "age", (DateTime.Now - fi.LastWriteTime).TotalDays, documentIndex);
                        IndexProperty(path, "dir", fi.Directory.Name.ToLowerInvariant(), documentIndex, saveKeyword: false);
                    }
                }

                var at = AssetDatabase.GetMainAssetTypeAtPath(path);
                var hasCustomIndexers = HasCustomIndexers(at);

                if (settings.options.properties || settings.options.types || hasCustomIndexers)
                {
                    bool wasLoaded = AssetDatabase.IsMainAssetAtPathLoaded(path);
                    var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                    if (!mainAsset)
                        return;

                    if (hasCustomIndexers)
                        IndexCustomProperties(path, documentIndex, mainAsset);

                    if (settings.options.properties || settings.options.types)
                    {
                        if (!String.IsNullOrEmpty(mainAsset.name))
                            IndexWord(path, mainAsset.name, documentIndex, true);

                        IndexWord(path, at.Name, documentIndex);
                        while (at != null && at != typeof(Object))
                        {
                            if (at != typeof(GameObject))
                                IndexProperty(path, "t", at.Name, documentIndex, saveKeyword: true);
                            at = at.BaseType;
                        }

                        var prefabType = PrefabUtility.GetPrefabAssetType(mainAsset);
                        if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
                            IndexProperty(path, "t", "prefab", documentIndex, saveKeyword: true);

                        var labels = AssetDatabase.GetLabels(mainAsset);
                        foreach (var label in labels)
                            IndexProperty(path, "l", label, documentIndex, saveKeyword: true);

                        if (settings.options.properties)
                            IndexObject(path, mainAsset, documentIndex);

                        if (mainAsset is GameObject go)
                        {
                            foreach (var v in go.GetComponents(typeof(Component)))
                            {
                                if (!v || v.GetType() == typeof(Transform))
                                    continue;
                                IndexPropertyComponents(path, documentIndex, "has", v.GetType().Name);
                                
                                if (settings.options.properties)
                                    IndexObject(path, v, documentIndex, dependencies: settings.options.dependencies);
                            }
                        }
                    }

                    if (!wasLoaded)
                    {
                        if (mainAsset && !mainAsset.hideFlags.HasFlag(HideFlags.DontUnloadUnusedAsset) &&
                            !(mainAsset is GameObject) &&
                            !(mainAsset is Component) &&
                            !(mainAsset is AssetBundle))
                        {
                            Resources.UnloadAsset(mainAsset);
                        }
                    }
                }

                if (settings.options.dependencies)
                {
                    foreach (var depPath in AssetDatabase.GetDependencies(path, true))
                    {
                        if (path == depPath)
                            continue;
                        var depName = Path.GetFileNameWithoutExtension(depPath);
                        IndexProperty(path, "ref", depName, documentIndex, saveKeyword: false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
