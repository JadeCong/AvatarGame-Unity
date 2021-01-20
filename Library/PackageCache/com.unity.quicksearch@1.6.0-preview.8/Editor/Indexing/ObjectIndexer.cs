//#define DEBUG_INDEXING

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.QuickSearch
{
    internal enum FilePattern
    {
        Extension,
        Folder,
        File
    }

    /// <summary>
    /// Descriptor for the object that is about to be indexed. It stores a reference to the object itself as well a an already setup SerializedObject.
    /// </summary>
    public struct CustomObjectIndexerTarget
    {
        /// <summary>
        /// Object to be indexed.
        /// </summary>
        public Object target;
        /// <summary>
        /// Serialized representation of the object to be indexed.
        /// </summary>
        public SerializedObject serializedObject;
        /// <summary>
        /// Object Id. It is the object path in case of an asset or the GlobalObjectId in terms of a scene object.
        /// </summary>
        public string id;
        /// <summary>
        /// Document Index owning the object to index.
        /// </summary>
        public int documentIndex;
        /// <summary>
        /// Type of the object to index.
        /// </summary>
        public Type targetType;
    }

    /// <summary>
    /// Allow a user to register a custom Indexing function for a specific type. The registered function must be of type: 
    /// static void Function(<see cref="CustomObjectIndexerTarget"/> context, <see cref="ObjectIndexer"/> indexer);
    /// <example>
    /// <code>
    /// [CustomObjectIndexer(typeof(Material))]
    /// internal static void MaterialShaderReferences(CustomObjectIndexerTarget context, ObjectIndexer indexer)
    /// {
    ///    var material = context.target as Material;
    ///    if (material == null)
    ///        return;
    ///
    ///    if (material.shader)
    ///    {
    ///        var fullShaderName = material.shader.name.ToLowerInvariant();
    ///        var shortShaderName = System.IO.Path.GetFileNameWithoutExtension(fullShaderName);
    ///        indexer.AddProperty("ref", shortShaderName, context.documentIndex, saveKeyword: false);
    ///        indexer.AddProperty("ref", fullShaderName, context.documentIndex, saveKeyword: false);
    ///    }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CustomObjectIndexerAttribute : Attribute
    {
        /// <summary>
        /// Each time an object of specific Type is indexed, the registered function will be called.
        /// </summary>
        public Type type { get; }

        /// <summary>
        /// Register a new Indexing function bound to the specific type.
        /// </summary>
        /// <param name="type">Type of object to be indexed.</param>
        public CustomObjectIndexerAttribute(Type type)
        {
            this.type = type;
        }
    }

    /// <summary>
    /// Specialized <see cref="SearchIndexer"/> used to index Unity Assets. See <see cref="AssetIndexer"/> for a specialized SearchIndexer used to index simple assets and
    /// see <see cref="SceneIndexer"/> for an indexer used to index scene and prefabs.
    /// </summary>
    public abstract class ObjectIndexer : SearchIndexer
    {
        private static readonly string[] k_FieldNamesNoKeywords = {"name", "text"};

        internal SearchDatabase.Settings settings { get; private set; }

        #if DEBUG_INDEXING
        private readonly Dictionary<string, HashSet<string>> m_DebugStringTable = new Dictionary<string, HashSet<string>>();
        #endif

        private readonly QueryEngine<SearchResult> m_QueryEngine = new QueryEngine<SearchResult>(validateFilters: false);
        private readonly Dictionary<string, Query<SearchResult, object>> m_QueryPool = new Dictionary<string, Query<SearchResult, object>>();
        private readonly Dictionary<Type, List<Action<CustomObjectIndexerTarget, ObjectIndexer>>> m_CustomObjectIndexers = new Dictionary<Type, List<Action<CustomObjectIndexerTarget, ObjectIndexer>>>();

        /// <summary>
        /// Event that triggers while indexing is happening to report progress. The event signature is
        /// <code>event(int progressId, string value, float progressReport, bool finished).</code>
        /// </summary>
        public event Action<int, string, float, bool> reportProgress;

        internal ObjectIndexer(string rootName, SearchDatabase.Settings settings)
            : base(rootName)
        {
            this.name = settings?.name ?? name;
            this.settings = settings;

            m_QueryEngine.SetSearchDataCallback(e => null);
            LoadCustomObjectIndexers();
        }

        /// <summary>
        /// Run a search query in the index.
        /// </summary>
        /// <param name="searchQuery">Search query to look out for. If if matches any of the indexed variations a result will be returned.</param>
        /// <param name="maxScore">Maximum score of any matched Search Result. See <see cref="SearchResult.score"/>.</param>
        /// <param name="patternMatchLimit">Maximum number of matched Search Result that can be returned. See <see cref="SearchResult"/>.</param>
        /// <returns>Returns a collection of Search Result matching the query.</returns>
        public override IEnumerable<SearchResult> Search(string searchQuery, int maxScore = int.MaxValue, int patternMatchLimit = 2999)
        {
            if (settings.options.disabled)
                return Enumerable.Empty<SearchResult>();

            var query = BuildQuery(searchQuery, maxScore, patternMatchLimit);
            if (!query.valid)
                return Enumerable.Empty<SearchResult>();

            #if DEBUG_INDEXING
            using (new DebugTimer($"Search \"{searchQuery}\" in {name}"))
            #endif
            {
                return query.Apply(null).OrderBy(e => e.score).Distinct();
            }
        }

        /// <summary>Build the index into a separate thread.</summary>
        public override void Build()
        {
            if (LoadIndexFromDisk(null, true))
                return;

            var it = BuildAsync(-1, null);
            while (it.MoveNext())
                ;
        }

        /// <summary>
        /// Called when the index is built to see if a specified document needs to be indexed. See <see cref="SearchIndexer.skipEntryHandler"/>
        /// </summary>
        /// <param name="path">Path of a document</param>
        /// <param name="checkRoots"></param>
        /// <returns>Returns true if the document doesn't need to be indexed.</returns>
        public override bool SkipEntry(string path, bool checkRoots = false)
        {
            if (checkRoots)
            {
                if (!GetRoots().Any(r => path.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            if (!settings.options.directories && Directory.Exists(path))
                return true;

            if (!settings.options.files && File.Exists(path))
                return true;

            var ext = Path.GetExtension(path);

            // Exclude indexes by default
            if (ext.EndsWith("index", StringComparison.OrdinalIgnoreCase))
                return true;

            var dir = Path.GetDirectoryName(path);

            if (settings.includes?.Length > 0 && !settings.includes.Any(pattern => PatternChecks(pattern, ext, dir, path)))
                return true;

            if (settings.excludes?.Length > 0 && settings.excludes.Any(pattern => PatternChecks(pattern, ext, dir, path)))
                return true;

            return false;
        }

        /// <summary>
        ///  Get all this indexer root paths.
        /// </summary>
        /// <returns>Returns a list of root paths.</returns>
        public abstract IEnumerable<string> GetRoots();
        /// <summary>
        /// Get all documents that would be indexed.
        /// </summary>
        /// <returns>Returns a list of file paths.</returns>
        public abstract List<string> GetDependencies();

        /// <summary>
        /// Compute the hash of a specific document id. Generally a file path.
        /// </summary>
        /// <param name="id">Document id.</param>
        /// <returns>Returns the hash of this document id.</returns>
        public abstract Hash128 GetDocumentHash(string id);

        /// <summary>
        /// Function to override in a concrete SearchIndexer to index the content of a document.
        /// </summary>
        /// <param name="id">Path of the document to index.</param>
        /// <param name="checkIfDocumentExists">Check if the document actually exists.</param>
        public abstract override void IndexDocument(string id, bool checkIfDocumentExists);

        /// <summary>
        /// Split a word into multiple components (see <see cref="SearchIndexer.getEntryComponentsHandler"/>) and index each component individually.
        /// </summary>
        /// <param name="id">Debug information</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        /// <param name="word">Word to add to the index.</param>
        public void IndexWordComponents(string id, int documentIndex, string word)
        {
            foreach (var c in GetComponents(word, documentIndex))
                IndexWord(id, c, documentIndex);
        }

        /// <summary>
        /// Split a value into multiple components (see <see cref="SearchIndexer.getEntryComponentsHandler"/>) and index each component individually as value of a specific property name.
        /// </summary>
        /// <param name="id">Debug information</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        /// <param name="name">Key used to retrieve the value. See <see cref="SearchIndexer.AddProperty"/></param>
        /// <param name="value">Value to add to the index.</param>
        public void IndexPropertyComponents(string id, int documentIndex, string name, string value)
        {
            foreach (var c in GetComponents(value, documentIndex))
            {
                IndexDebugMatch(id, name, c);
                AddProperty(name, c, settings.baseScore, documentIndex, saveKeyword: true, exact: false);
            }
            IndexDebugMatch(id, name, value);
            AddExactProperty(name, value.ToLowerInvariant(), settings.baseScore, documentIndex, saveKeyword: false);
        }

        /// <summary>
        /// Add a new word coming from a specific document to the index. The word will be added with multiple variations allowing partial search. See <see cref="SearchIndexer.AddWord"/>.
        /// </summary>
        /// <param name="id">Debug information</param>
        /// <param name="word">Word to add to the index.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        /// <param name="maxVariations">Maximum number of variations to compute. Cannot be higher than the length of the word.</param>
        /// <param name="exact">If true, we will store also an exact match entry for this word.</param>
        public void IndexWord(string id, string word, int documentIndex, int maxVariations, bool exact)
        {
            IndexDebugMatch(id, word);
            AddWord(word.ToLowerInvariant(), 2, maxVariations, settings.baseScore, documentIndex);
            if (exact)
                AddExactWord(word.ToLowerInvariant(), settings.baseScore - 1, documentIndex);
        }

        /// <summary>
        /// Add a new word coming from a specific document to the index. The word will be added with multiple variations allowing partial search. See <see cref="SearchIndexer.AddWord"/>.
        /// </summary>
        /// <param name="id">Debug information</param>
        /// <param name="word">Word to add to the index.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        /// <param name="exact">If true, we will store also an exact match entry for this word.</param>
        public void IndexWord(string id, string word, int documentIndex, bool exact = false)
        {
            IndexWord(id, word, documentIndex, word.Length, exact);
        }

        /// <summary>
        /// Add a property value to the index. A property is specified with a key and a string value. The value will be stored with multiple variations. See <see cref="SearchIndexer.AddProperty"/>.
        /// </summary>
        /// <param name="id">Debug information</param>
        /// <param name="name">Key used to retrieve the value. See <see cref="SearchIndexer.AddProperty"/></param>
        /// <param name="value">Value to add to the index.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        /// <param name="saveKeyword">Define if we store this key in the keyword registry of the index. See <see cref="SearchIndexer.GetKeywords"/>.</param>
        /// <param name="exact">If exact is true, only the exact match of the value will be stored in the index (not the variations).</param>
        public void IndexProperty(string id, string name, string value, int documentIndex, bool saveKeyword, bool exact = false)
        {
            if (String.IsNullOrEmpty(value))
                return;
            IndexDebugMatch(id, name, value);
            var valueLower = value.ToLowerInvariant();
            if (exact)
            {
                AddProperty(name, valueLower, valueLower.Length, valueLower.Length, settings.baseScore, documentIndex, saveKeyword: saveKeyword, exact: true);
            }
            else
                AddProperty(name, valueLower, settings.baseScore, documentIndex, saveKeyword: saveKeyword);
        }
        /// <summary>
        /// Add a key-number value pair to the index. The key won't be added with variations. See <see cref="SearchIndexer.AddNumber"/>.
        /// </summary>
        /// <param name="id">Debug information</param>
        /// <param name="name">Key used to retrieve the value.</param>
        /// <param name="number">Number value to store in the index.</param>
        /// <param name="documentIndex">Document where the indexed value was found.</param>
        public void IndexNumber(string id, string name, double number, int documentIndex)
        {
            IndexDebugMatch(id, name, number.ToString());
            AddNumber(name, number, settings.baseScore, documentIndex);
        }
        /// <summary>
        /// Report progress of indexing.
        /// </summary>
        /// <param name="progressId">Progress id.</param>
        /// <param name="value">Progress description.</param>
        /// <param name="progressReport">Progress report value (between 0 and 1).</param>
        /// <param name="finished">Is the indexing done?</param>
        protected void ReportProgress(int progressId, string value, float progressReport, bool finished)
        {
            reportProgress?.Invoke(progressId, value, progressReport, finished);
        }
        /// <summary>
        /// Build Index asynchronously (in a thread).
        /// </summary>
        /// <param name="progressId">Id to use to report progress. See <see cref="ObjectIndexer.ReportProgress"/></param>
        /// <param name="userData">User data pass to the indexing process.</param>
        /// <returns>Returns enumerator during the asynchronous build.</returns>
        protected abstract System.Collections.IEnumerator BuildAsync(int progressId, object userData = null);

        private Query<SearchResult, object> BuildQuery(string searchQuery, int maxScore, int patternMatchLimit)
        {
            Query<SearchResult, object> query;
            if (m_QueryPool.TryGetValue(searchQuery, out query) && query.valid)
                return query;

            if (m_QueryPool.Count > 50)
                m_QueryPool.Clear();

            query = m_QueryEngine.Parse(searchQuery, new SearchIndexerQueryFactory(args =>
            {
                if (args.op == SearchIndexOperator.None)
                    return SearchIndexerQuery.EvalResult.None;

                #if DEBUG_INDEXING
                using (var t = new DebugTimer(null))
                #endif
                {
                    SearchResultCollection subset = null;
                    if (args.andSet != null)
                        subset = new SearchResultCollection(args.andSet);

                    var results = SearchTerm(args.name, args.value, args.op, args.exclude, maxScore, subset, patternMatchLimit);
                    if (args.orSet != null)
                        results = results.Concat(args.orSet);

                    #if DEBUG_INDEXING
                    SearchIndexerQuery.EvalResult.Print(args, results, subset, t.timeMs);
                    #endif
                    return SearchIndexerQuery.EvalResult.Combined(results);
                }
            }));
            if (query.valid)
                m_QueryPool[searchQuery] = query;
            return query;
        }

        internal static FilePattern GetFilePattern(string pattern)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                if (pattern[0] == '.')
                    return FilePattern.Extension;
                if (pattern[pattern.Length - 1] == '/')
                    return FilePattern.Folder;
            }
            return FilePattern.File;
        }

        private bool PatternChecks(string pattern, string ext, string dir, string fileName)
        {
            var filePattern = GetFilePattern(pattern);
            // Extension check
            if (filePattern == FilePattern.Extension && ext.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                return true;

            // Folder check
            if (filePattern == FilePattern.Folder)
            {
                var icDir = pattern.Substring(0, pattern.Length - 1);
                if (dir.IndexOf(icDir, StringComparison.OrdinalIgnoreCase) != -1)
                    return true;
            }

            // File name check
            if (filePattern == FilePattern.File && fileName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) != -1)
                return true;

            return false;
        }

        private string[] GetComponents(string id, int documentIndex)
        {
            return getEntryComponentsHandler(id, documentIndex).Where(c => c.Length > 0).ToArray();
        }

        /// <summary>
        /// Store a key value pair in the debug string table of the Indexer. This is an internal QuickSearch debugging function.
        /// </summary>
        /// <param name="id">Entry id</param>
        /// <param name="name">Key used to retrieve the value. See <see cref="SearchIndexer.AddProperty"/></param>
        /// <param name="value">Value to add to the index.</param>
        [System.Diagnostics.Conditional("DEBUG_INDEXING")]
        protected void IndexDebugMatch(string id, string name, string value)
        {
            IndexDebugMatch(id, $"{name}:{value}");
        }

        /// <summary>
        /// Store a key value pair in the debug string table of the Indexer. This is an internal QuickSearch debugging function.
        /// </summary>
        /// <param name="id">Entry id</param>
        /// <param name="word">Word to index.</param>
        [System.Diagnostics.Conditional("DEBUG_INDEXING")]
        protected void IndexDebugMatch(string id, string word)
        {
            #if DEBUG_INDEXING
            HashSet<string> words;
            if (m_DebugStringTable.TryGetValue(id, out words))
            {
                words.Add(word);
            }
            else
            {
                m_DebugStringTable[id] = new HashSet<string> { word };
            }
            #endif
        }

        internal string GetDebugIndexStrings(string id)
        {
            #if DEBUG_INDEXING
            if (!m_DebugStringTable.ContainsKey(id))
                return null;

            return String.Join(" ", m_DebugStringTable[id].ToArray());
            #else
            return null;
            #endif
        }
        /// <summary>
        /// Get the property value of a specific property. This will converts the property to either a double or a string.
        /// </summary>
        /// <param name="property">Property to get value from.</param>
        /// <param name="saveKeyword">If set to true, this means we need to save the property name in the index.</param>
        /// <returns>Property value as a double or a string or null if we weren't able to convert it.</returns>
        protected object GetPropertyValue(SerializedProperty property, ref bool saveKeyword)
        {
            object fieldValue = null;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    fieldValue = (double)property.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    fieldValue = property.boolValue.ToString();
                    break;
                case SerializedPropertyType.Float:
                    fieldValue = (double)property.floatValue;
                    break;
                case SerializedPropertyType.String:
                    if (property.stringValue != null && property.stringValue.Length < 10)
                        fieldValue = property.stringValue.Replace(" ", "").ToString();
                    break;
                case SerializedPropertyType.Enum:
                    if (property.enumValueIndex >= 0 && property.type == "Enum")
                        fieldValue = property.enumNames[property.enumValueIndex].ToString();
                    break;
                case SerializedPropertyType.ObjectReference:
                    if (property.objectReferenceValue)
                    {
                        saveKeyword = false;
                        fieldValue = property.objectReferenceValue.name.Replace(" ", "");
                    }
                    break;
                default:
                    break;
            }

            return fieldValue;
        }

        /// <summary>
        /// Index all the properties of an object.
        /// </summary>
        /// <param name="id">Object id.</param>
        /// <param name="obj">Object to index.</param>
        /// <param name="documentIndex">Document where the indexed object was found.</param>
        /// <param name="dependencies">Index dependencies.</param>
        protected void IndexObject(string id, Object obj, int documentIndex, bool dependencies = false)
        {
            using (var so = new SerializedObject(obj))
            {
                var p = so.GetIterator();
                var next = p.Next(true);
                while (next)
                {
                    bool saveKeyword = true;
                    var fieldName = p.displayName.Replace("m_", "").Replace(" ", "").ToLowerInvariant();
                    var scc = SearchUtils.SplitCamelCase(fieldName);
                    var fcc = scc.Length > 1 && fieldName.Length > 10 ? scc.Aggregate("", (current, s) => current + s[0]) : fieldName;
                    object fieldValue = GetPropertyValue(p, ref saveKeyword);

                    // Some property names are not worth indexing and take to much spaces.
                    if (k_FieldNamesNoKeywords.Contains(fieldName))
                        saveKeyword = false;

                    if (fieldValue != null)
                    {
                        var sfv = fieldValue as string;
                        if (sfv != null)
                        {
                            if (sfv != "")
                                IndexProperty(id, fcc, sfv.Replace(" ", "").ToLowerInvariant(), documentIndex, saveKeyword);
                            else
                                IndexWord(id, $"@{fcc}", documentIndex);
                        }
                        else if (fieldValue is double)
                        {
                            var nfv = (double)fieldValue;
                            IndexNumber(id, fcc.ToLowerInvariant(), nfv, documentIndex);
                        }

                        IndexDebugMatch(id, fcc, fieldValue.ToString());
                    }

                    if (dependencies)
                        AddReference(id, p, documentIndex);

                    next = p.Next(p.hasVisibleChildren);
                }
            }
        }

        private void AddReference(string id, SerializedProperty p, int documentIndex)
        {
            if (p.propertyType != SerializedPropertyType.ObjectReference || !p.objectReferenceValue)
                return;

            var refValue = AssetDatabase.GetAssetPath(p.objectReferenceValue);
            if (!String.IsNullOrEmpty(refValue))
            {
                refValue = refValue.ToLowerInvariant();
                IndexProperty(id, "ref", refValue, documentIndex, saveKeyword: false);
                IndexProperty(id, "ref", Path.GetFileName(refValue), documentIndex, saveKeyword: false);
            }
        }

        private void LoadCustomObjectIndexers()
        {
            var customIndexerMethodInfos = Utils.GetAllMethodsWithAttribute<CustomObjectIndexerAttribute>();
            foreach (var customIndexerMethodInfo in customIndexerMethodInfos)
            {
                var customIndexerAttribute = customIndexerMethodInfo.GetCustomAttribute<CustomObjectIndexerAttribute>();
                var indexerType = customIndexerAttribute.type;
                if (indexerType == null)
                    continue;

                if (!ValidateCustomIndexerMethodSignature(customIndexerMethodInfo))
                    continue;

                var customIndexerAction = Delegate.CreateDelegate(typeof(Action<CustomObjectIndexerTarget, ObjectIndexer>), customIndexerMethodInfo) as Action<CustomObjectIndexerTarget, ObjectIndexer>;
                if (customIndexerAction == null)
                    continue;

                if (!m_CustomObjectIndexers.TryGetValue(indexerType, out var indexerList))
                {
                    indexerList = new List<Action<CustomObjectIndexerTarget, ObjectIndexer>>();
                    m_CustomObjectIndexers.Add(indexerType, indexerList);
                }
                indexerList.Add(customIndexerAction);
            }
        }

        private static bool ValidateCustomIndexerMethodSignature(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                return false;

            if (methodInfo.ReturnType != typeof(void))
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, $"Method \"{methodInfo.Name}\" must return void.");
                return false;
            }

            var paramTypes = new[] { typeof(CustomObjectIndexerTarget), typeof(ObjectIndexer) };
            var parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length != paramTypes.Length)
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, $"Method \"{methodInfo.Name}\" must have {paramTypes.Length} parameter{(paramTypes.Length > 1 ? "s" : "")}.");
                return false;
            }

            for (var i = 0; i < paramTypes.Length; ++i)
            {
                if (parameterInfos[i].ParameterType != paramTypes[i])
                {
                    Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, $"The parameter \"{parameterInfos[i].Name}\" of method \"{methodInfo.Name}\" must be of type \"{paramTypes[i]}\".");
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        /// Call all the registered custom indexer for a specific object. See <see cref="CustomObjectIndexerAttribute"/>.
        /// </summary>
        /// <param name="id">Object id.</param>
        /// <param name="obj">Object to index.</param>
        /// <param name="documentIndex">Document where the indexed object was found.</param>
        protected void IndexCustomProperties(string id, int documentIndex, Object obj)
        {
            using (var so = new SerializedObject(obj))
            {
                CallCustomIndexers(id, obj, documentIndex, so);
            }
        }
        /// <summary>
        /// Call all the registered custom indexer for an object of a specific type. See <see cref="CustomObjectIndexerAttribute"/>.
        /// </summary>
        /// <param name="id">Object id.</param>
        /// <param name="obj">Object to index.</param>
        /// <param name="documentIndex">Document where the indexed object was found.</param>
        /// <param name="so">SerializedObject representation of obj.</param>
        /// <param name="multiLevel">If true, calls all the indexer that would fit the type of the object (all assignable type). If false only check for an indexer registered for the exact type of the Object.</param>
        protected void CallCustomIndexers(string id, Object obj, int documentIndex, SerializedObject so, bool multiLevel = true)
        {
            var objectType = obj.GetType();
            List<Action<CustomObjectIndexerTarget, ObjectIndexer>> customIndexers;
            if (!multiLevel)
            {
                if (!m_CustomObjectIndexers.TryGetValue(objectType, out customIndexers))
                    return;
            }
            else
            {
                customIndexers = new List<Action<CustomObjectIndexerTarget, ObjectIndexer>>();
                var indexerTypes = m_CustomObjectIndexers.Keys;
                foreach (var indexerType in indexerTypes)
                {
                    if (indexerType.IsAssignableFrom(objectType))
                        customIndexers.AddRange(m_CustomObjectIndexers[indexerType]);
                }
            }

            var indexerTarget = new CustomObjectIndexerTarget
            {
                id = id,
                documentIndex = documentIndex,
                target = obj,
                serializedObject = so,
                targetType = objectType
            };

            foreach (var customIndexer in customIndexers)
            {
                customIndexer(indexerTarget, this);
            }
        }

        internal bool HasCustomIndexers(Type type, bool multiLevel = true)
        {
            if (!multiLevel)
                return m_CustomObjectIndexers.ContainsKey(type);

            var indexerTypes = m_CustomObjectIndexers.Keys;
            foreach (var indexerType in indexerTypes)
            {
                if (indexerType.IsAssignableFrom(type))
                    return true;
            }
            return false;
        }
    }
}
