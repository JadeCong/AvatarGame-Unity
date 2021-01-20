//#define DEBUG_INDEXING

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.QuickSearch
{
    class SceneIndexer : ObjectIndexer
    {
        public SceneIndexer(SearchDatabase.Settings settings)
            : base("objects", settings)
        {
            getEntryComponentsHandler = GetEntryComponents;
        }

        public IEnumerable<string> GetEntryComponents(string path, int index)
        {
            return SearchUtils.SplitFileEntryComponents(path, entrySeparators);
        }

        public override bool SkipEntry(string path, bool checkRoots = false)
        {
            if (checkRoots && !GetRoots().Any(p => p == path))
                return true;
            return base.SkipEntry(path, false);
        }

        protected override System.Collections.IEnumerator BuildAsync(int progressId, object userData = null)
        {
            Start(clear: true);

            var assetPaths = GetDependencies();
            var itIndex = 0;
            var iterationCount = (float)assetPaths.Count;
            foreach (var scenePath in assetPaths)
            {
                var progressReport = itIndex++ / iterationCount;
                ReportProgress(progressId, scenePath, progressReport, false);
                try
                {
                    IndexDocument(scenePath, false);
                }
                catch (Exception ex)
                {
                    Debug.LogFormat(LogType.Exception, LogOption.NoStacktrace, null,
                        $"Failed to index scene {scenePath}.\r\n{ex.ToString()}");
                }
                yield return null;
            }

            Finish((bytes) => {}, null, false);
            while (!IsReady())
                yield return null;

            ReportProgress(progressId, $"Scene indexing completed (Objects: {documentCount}, Indexes: {indexCount:n0})", 1f, true);
        }

        public override IEnumerable<string> GetRoots()
        {
            var scenePaths = new List<string>();
            if (settings.roots == null || settings.roots.Length == 0)
            {
                var sceneDirPath = Path.GetDirectoryName(settings.path).Replace("\\", "/");
                scenePaths.AddRange(AssetDatabase.FindAssets("t:" + settings.type, new string[] { sceneDirPath }).Select(AssetDatabase.GUIDToAssetPath));
            }
            else
            {
                foreach (var path in settings.roots)
                {
                    if (File.Exists(path))
                        scenePaths.Add(path);
                    else if (Directory.Exists(path))
                        scenePaths.AddRange(AssetDatabase.FindAssets("t:" + settings.type, new string[] { path }).Select(AssetDatabase.GUIDToAssetPath));
                }
            }

            return scenePaths;
        }

        public override List<string> GetDependencies()
        {
            return GetRoots().Where(path => !base.SkipEntry(path, false)).ToList();
        }

        public override Hash128 GetDocumentHash(string path)
        {
            return AssetDatabase.GetAssetDependencyHash(path);
        }

        public override void IndexDocument(string scenePath, bool checkIfDocumentExists)
        {
            if (scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                IndexScene(scenePath, checkIfDocumentExists);
            else if (scenePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                IndexPrefab(scenePath, checkIfDocumentExists);
            AddDocumentHash(scenePath, GetDocumentHash(scenePath));
        }

        private void IndexObjects(GameObject[] objects, string type, string containerName, bool checkIfDocumentExists)
        {
            var options = settings.options;
            var globalIds = new GlobalObjectId[objects.Length];
            GlobalObjectId.GetGlobalObjectIdsSlow(objects, globalIds);

            for (int i = 0; i < objects.Length; ++i)
            {
                var obj = objects[i];
                if (!obj)
                    continue;

                if (PrefabUtility.IsPrefabAssetMissing(obj))
                    continue;

                var gid = globalIds[i];
                var id = gid.ToString();
                var path = SearchUtils.GetTransformPath(obj.transform);
                var documentIndex = AddDocument(id, path, checkIfDocumentExists);

                if (!String.IsNullOrEmpty(name))
                    IndexProperty(id, "a", name, documentIndex, saveKeyword: true);

                var depth = GetObjectDepth(obj);
                IndexNumber(id, "depth", depth, documentIndex);

                IndexWordComponents(id, documentIndex, path);
                IndexProperty(id, "from", type, documentIndex, saveKeyword: true, exact: true);
                IndexProperty(id, type, containerName, documentIndex, saveKeyword: true);
                IndexGameObject(id, documentIndex, obj, options);
                IndexCustomGameObjectProperties(id, documentIndex, obj);
            }
        }

        private int GetObjectDepth(GameObject obj)
        {
            int depth = 1;
            var transform = obj.transform;
            while (transform != null && transform.root != transform)
            {
                ++depth;
                transform = transform.parent;
            }
            return depth;
        }

        private void IndexPrefab(string prefabPath, bool checkIfDocumentExists)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (!prefabRoot)
                return;
            try
            {
                var objects = SceneModeUtility.GetObjects(new[] { prefabRoot }, true);
                IndexObjects(objects, "prefab", prefabRoot.name, checkIfDocumentExists);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private void IndexScene(string scenePath, bool checkIfDocumentExists)
        {
            bool sceneAdded = false;
            var scene = EditorSceneManager.GetSceneByPath(scenePath);
            try
            {
                if (scene == null || !scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    sceneAdded = scene != null && scene.isLoaded;
                }

                if (scene == null || !scene.isLoaded)
                    return;

                var objects = FetchGameObjects(scene);
                IndexObjects(objects, "scene", scene.name, checkIfDocumentExists);
            }
            finally
            {
                if (sceneAdded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private void IndexGameObject(string id, int documentIndex, GameObject go, SearchDatabase.Options options)
        {
            if (options.fstats)
            {
                if (go.transform.root != go.transform)
                    IndexProperty(id, "is", "child", documentIndex, saveKeyword: true, exact: true);
                else
                    IndexProperty(id, "is", "root", documentIndex, saveKeyword: true, exact: true);

                if (go.transform.childCount == 0)
                    IndexProperty(id, "is", "leaf", documentIndex, saveKeyword: true, exact: true);

                IndexNumber(id, "layer", go.layer, documentIndex);
                IndexProperty(id, "tag", go.tag, documentIndex, saveKeyword: true);
            }

            if (options.types || options.properties || options.dependencies)
            {
                if (options.types)
                {
                    var ptype = PrefabUtility.GetPrefabAssetType(go);
                    if (ptype == PrefabAssetType.Regular || ptype == PrefabAssetType.Variant)
                    {
                        IndexProperty(id, "t", "prefab", documentIndex, saveKeyword: true, exact: true);

                        if (options.dependencies)
                        {
                            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                            var prefabName = Path.GetFileNameWithoutExtension(prefabPath);
                            IndexProperty(id, "ref", prefabName, documentIndex, saveKeyword: true);
                        }
                    }
                }

                if (options.properties)
                    IndexObject(id, go, documentIndex);

                var gocs = go.GetComponents<Component>();

                IndexNumber(id, "components", gocs.Length, documentIndex);

                for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
                {
                    var c = gocs[componentIndex];
                    if (!c || c.hideFlags.HasFlag(HideFlags.HideInInspector))
                        continue;

                    if (options.types)
                        IndexProperty(id, "t", c.GetType().Name.ToLowerInvariant(), documentIndex, saveKeyword: true);

                    if (options.properties)
                        IndexObject(id, c, documentIndex);
                }
            }
        }

        private void IndexCustomGameObjectProperties(string id, int documentIndex, GameObject go)
        {
            if (HasCustomIndexers(go.GetType()))
                IndexCustomProperties(id, documentIndex, go);

            var gocs = go.GetComponents<Component>();
            // Why begin at 1?
            for (var componentIndex = 0; componentIndex < gocs.Length; ++componentIndex)
            {
                var c = gocs[componentIndex];
                // Should we skip HideInInspector components?
                if (!c)
                    continue;
                if (HasCustomIndexers(c.GetType()))
                    IndexCustomProperties(id, documentIndex, c);
            }
        }

        public static GameObject[] FetchGameObjects(Scene scene)
        {
            var goRoots = new List<Object>();
            if (!scene.IsValid() || !scene.isLoaded)
                return new GameObject[0];
            var sceneRootObjects = scene.GetRootGameObjects();
            if (sceneRootObjects != null && sceneRootObjects.Length > 0)
                goRoots.AddRange(sceneRootObjects);

            return SceneModeUtility.GetObjects(goRoots.ToArray(), true)
                .Where(o => !o.hideFlags.HasFlag(HideFlags.HideInHierarchy)).ToArray();
        }
    }
}
