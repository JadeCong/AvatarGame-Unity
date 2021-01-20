//#define DEBUG_EXPRESSION_SEARCH

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.QuickSearch
{
    class SearchRequest : IDisposable
    {
        public string id { get; private set; }
        public bool resolving { get; private set; }
        public ExpressionType type { get; private set; }
        public List<SearchRequest> dependencies { get; private set; }
        public SearchContext context => runningQueries.LastOrDefault() ?? pendingQueries.FirstOrDefault();

        public Queue<SearchContext> pendingQueries { get; private set; }
        public List<SearchContext> runningQueries { get; private set; }

        private Func<SearchItem, IEnumerable<SearchItem>> selectCallback;

        public static readonly SearchRequest empty = new SearchRequest(ExpressionType.Undefined);
        private static Dictionary<string, Type> s_TypeTable = new Dictionary<string, Type>();

        /// <summary>
        /// Called every time new results are available.
        /// </summary>
        public event Action<IEnumerable<SearchItem>> resultsReceived;

        /// <summary>
        /// Called when the search expression has resolved.
        /// </summary>
        public event Action<SearchRequest> resolved;

        public SearchRequest(ExpressionType type, Action<SearchRequest> finishedHandler = null)
        {
            id = Guid.NewGuid().ToString();
            this.type = type;
            pendingQueries = new Queue<SearchContext>();
            runningQueries = new List<SearchContext>();
            dependencies = new List<SearchRequest>();
            resolving = false;

            if (finishedHandler != null)
                resolved += finishedHandler;
        }

        public SearchRequest(ExpressionType type, SearchContext searchContext, Action<SearchRequest> finishedHandler = null)
            : this(type, finishedHandler)
        {
            pendingQueries.Enqueue(searchContext);
        }

        public void Dispose()
        {
            foreach (var r in runningQueries.ToArray())
                OnSearchEnded(r);
            resultsReceived = null;
        }

        public SearchRequest Join(Func<string, SearchRequest> handler)
        {
            SearchRequest joinedRequest = new SearchRequest(type);

            Resolve(results =>
            {
                foreach (var item in results)
                {
                    if (item == null)
                        continue;

                    var request = handler(item.id);
                    request.TransferTo(joinedRequest);
                }
            }, null);

            DependsOn(joinedRequest);
            return joinedRequest;
        }

        public SearchRequest Select(ExpressionSelectField selectField, string objectType, string propertyName)
        {
            selectCallback = (item) =>
            {
                switch (selectField)
                {
                    case ExpressionSelectField.Default:
                        return new SearchItem[] { item };

                    case ExpressionSelectField.Type:
                        return SelectTypes(item);

                    case ExpressionSelectField.Component:
                        return SelectComponent(item, objectType, propertyName);

                    case ExpressionSelectField.Object:
                        return SelectObject(item, objectType, propertyName);
                }

                return Enumerable.Empty<SearchItem>();
            };

            return this;
        }

        private ISet<SearchItem> SelectTypes(SearchItem item)
        {
            var results = new HashSet<SearchItem>();
            var obj = item.provider.toObject?.Invoke(item, typeof(UnityEngine.Object));
            if (!obj)
                return results;

            if (obj is GameObject go)
                results.UnionWith(go.GetComponents<Component>().Select(c => new SearchItem(c.GetType().Name)));
            else
                results.Add(new SearchItem(obj.GetType().Name));

            return results;
        }

        private ISet<SearchItem> SelectObject(SearchItem item, string objectTypeName, string objectPropertyName)
        {
            var results = new HashSet<SearchItem>();

            if (objectTypeName == null || objectPropertyName == null)
                return results;

            if (!s_TypeTable.TryGetValue(objectTypeName ?? "", out var objectType))
            {
                objectType = TypeCache.GetTypesDerivedFrom(typeof(UnityEngine.Object)).FirstOrDefault(t => t.Name == objectTypeName);
                s_TypeTable[objectTypeName] = objectType;
            }

            if (objectType == null)
                return results;

            if (typeof(Component).IsAssignableFrom(objectType))
                return SelectComponent(item, objectTypeName, objectPropertyName);

            var assetObject = item.provider?.toObject(item, objectType);
            if (!assetObject)
                return results;

            SelectProperty(assetObject, objectPropertyName, results);

            return results;
        }

        private ISet<SearchItem> SelectComponent(SearchItem item, string objectTypeName, string objectPropertyName)
        {
            var results = new HashSet<SearchItem>();

            if (objectTypeName == null || objectPropertyName == null)
                return results;

            var go = item.provider?.toObject(item, typeof(GameObject)) as GameObject;
            if (!go)
                return results;

            var correspondingObject = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go) ?? go;
            if (!correspondingObject)
                return results;

            if (!s_TypeTable.TryGetValue(objectTypeName ?? "", out var objectType))
            {
                objectType = TypeCache.GetTypesDerivedFrom<Component>().FirstOrDefault(t => t.Name == objectTypeName);
                s_TypeTable[objectTypeName] = objectType;
            }

            if (objectType == null)
                return results;

            var components = correspondingObject.GetComponentsInChildren(objectType);
            foreach (var c in components)
            {
                if (!c)
                    continue;
                SelectProperty(c, objectPropertyName, results);
            }

            return results;
        }

        private void SelectProperty(UnityEngine.Object obj, string objectPropertyName, ISet<SearchItem> results)
        {
            using (var so = new SerializedObject(obj))
            {
                var property = so.FindProperty(objectPropertyName);
                if (property == null || property.isArray)
                    return;
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer: SelectValue(results, property.intValue); break;
                    case SerializedPropertyType.Enum: SelectValue(results, property.enumNames[property.enumValueIndex]); break;
                    case SerializedPropertyType.Boolean: SelectValue(results, property.boolValue.ToString().ToLowerInvariant()); break;
                    case SerializedPropertyType.String: SelectValue(results, property.stringValue); break;
                    case SerializedPropertyType.Float: SelectValue(results, property.floatValue); break;
                    case SerializedPropertyType.FixedBufferSize: SelectValue(results, property.fixedBufferSize); break;
                    case SerializedPropertyType.Color: SelectValue(results, ColorUtility.ToHtmlStringRGB(property.colorValue)); break;
                    case SerializedPropertyType.AnimationCurve: SelectValue(results, property.animationCurveValue); break;

                    case SerializedPropertyType.Vector2: SelectVector(results, property.vector2Value); break;
                    case SerializedPropertyType.Vector3: SelectVector(results, property.vector3Value); break;
                    case SerializedPropertyType.Vector4: SelectVector(results, property.vector4Value); break;
                    case SerializedPropertyType.Rect: SelectVector(results, property.rectValue); break;
                    case SerializedPropertyType.Bounds: SelectVector(results, property.boundsValue); break;
                    case SerializedPropertyType.Quaternion: SelectVector(results, property.quaternionValue); break;
                    case SerializedPropertyType.Vector2Int: SelectVector(results, property.vector2IntValue); break;
                    case SerializedPropertyType.Vector3Int: SelectVector(results, property.vector3IntValue); break;
                    case SerializedPropertyType.RectInt: SelectVector(results, property.rectIntValue); break;
                    case SerializedPropertyType.BoundsInt: SelectVector(results, property.boundsIntValue); break;

                    case SerializedPropertyType.ManagedReference: SelectValue(results, property.managedReferenceFullTypename); break;
                    case SerializedPropertyType.ObjectReference: SelectAssetPath(results, property.objectReferenceValue); break;
                    case SerializedPropertyType.ExposedReference: SelectAssetPath(results, property.exposedReferenceValue); break;

                    case SerializedPropertyType.Generic:
                        break;

                    case SerializedPropertyType.Gradient:
                    case SerializedPropertyType.Character:
                    case SerializedPropertyType.LayerMask:
                    case SerializedPropertyType.ArraySize:
                    default:
                        Debug.LogWarning($"Cannot select {property.propertyType} {objectPropertyName} with {id}");
                        break;
                }
            }
        }

        private void SelectVector<T>(ISet<SearchItem> results, T value) where T : struct
        {
            results.Add(new SearchItem(Convert.ToString(value).Replace("(", "").Replace(")", "").Replace(" ", "")));
        }

        private void SelectValue(ISet<SearchItem> results, object value)
        {
            results.Add(new SearchItem(Convert.ToString(value)));
        }

        private void SelectAssetPath(ISet<SearchItem> results, UnityEngine.Object obj)
        {
            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (String.IsNullOrEmpty(assetPath))
                return;
            results.Add(new SearchItem(assetPath));
        }

        public void Resolve(Action<IEnumerable<SearchItem>> onSearchItemReceived, Action<SearchRequest> finishedHandler)
        {
            if (onSearchItemReceived != null)
                resultsReceived += onSearchItemReceived;

            if (finishedHandler != null)
                resolved += finishedHandler;

            DebugLog($"Resolving <b>{type}</b>");
            resolving = true;
            while (pendingQueries.Count > 0)
            {
                var r = pendingQueries.Dequeue();
                runningQueries.Add(r);

                r.sessionEnded += OnSearchEnded;
                r.asyncItemReceived += OnSearchItemsReceived;

                DebugLog($"Fetching items with <a>{r.searchQuery}</a>");
                SearchService.GetItems(r, SearchFlags.FirstBatchAsync);

                if (!r.searchInProgress)
                    runningQueries.Remove(context);
            }

            UpdateState();

            // Check if we still have some pending queries to resolve.
            if (resolving)
            {
                EditorApplication.update -= DeferredResolve;
                EditorApplication.update += DeferredResolve;
            }
        }

        private void DeferredResolve()
        {
            EditorApplication.update -= DeferredResolve;
            Resolve(null, null);
        }

        [System.Diagnostics.Conditional("DEBUG_EXPRESSION_SEARCH")]
        private void DebugLog(string msg)
        {
            UnityEngine.Debug.Log($"<i>[{id}]</i> {msg}");
        }

        public void ProcessItems(SearchContext context, IEnumerable<SearchItem> items)
        {
            if (items is ICollection list)
            {
                if (list.Count > 0)
                    DebugLog($"Received {list.Count} items for {context?.searchQuery ?? type.ToString()}");
            }
            else
                DebugLog($"Received more items for {context?.searchQuery ?? type.ToString()}");

            if (resultsReceived == null)
                return;

            if (selectCallback == null)
                resultsReceived(items);
            else
                resultsReceived(items.SelectMany(item => selectCallback(item).Where(i => i != null)).Distinct());
        }

        private void OnSearchEnded(SearchContext context)
        {
            context.sessionEnded -= OnSearchEnded;
            context.asyncItemReceived -= OnSearchItemsReceived;
            runningQueries.Remove(context);

            UpdateState();
        }

        private void UpdateState()
        {
            if (!resolving)
                return;

            if (dependencies.Count == 0 && runningQueries.Count == 0 && pendingQueries.Count == 0)
            {
                resolving = false;
                resolved?.Invoke(this);
                DebugLog($"Resolved <b>{type}</b>");

                resolved = null;
                resultsReceived = null;
            }
        }

        private void OnSearchItemsReceived(SearchContext context, IEnumerable<SearchItem> items)
        {
            ProcessItems(context, items);
        }

        public void DependsOn(SearchRequest exs)
        {
            dependencies.Add(exs);
            exs.resolved += OnDependencyFinished;
        }

        private void OnDependencyFinished(SearchRequest exs)
        {
            exs.resolved -= OnDependencyFinished;
            dependencies.Remove(exs);
            UpdateState();
        }

        public void TransferTo(SearchRequest exSearch)
        {
            while (pendingQueries.Count > 0)
                exSearch.pendingQueries.Enqueue(pendingQueries.Dequeue());
        }
    }
}
