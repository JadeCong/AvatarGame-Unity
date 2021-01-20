using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Unity.QuickSearch
{
    enum ExpressionType
    {
        Undefined = -1,
        Provider,         // Provide data from a search provider
        Value,            // Provide a dynamic or value value (i.e. function, blackboard, etc.) for a search variable
        Search,           // Evaluate a search request
        Union,            // Merge two search results together.
        Intersect,        // Intersect results from two sources.
        Except,           // Produces the set difference of two sequences.
        Select,           // Select operation to output specific value into a variable.
        Expression,       // Nested expressions
        Results           // Final search expression results node.
    }

    /// <summary>
    /// A Search Expression can be populated with variables and providers. It can then be evaluated to yield a list of <see cref="SearchItem"/>
    /// </summary>
    public interface ISearchExpression : ISearchList
    {
        /// <summary>
        /// Evaluate the SearchExpression.
        /// </summary>
        /// <returns>Returns the list of Items.</returns>
        ISearchList Evaluate();

        /// <summary>
        /// Set the value of a variable with a specific name.
        /// </summary>
        /// <param name="name">Name of the variable to assign to.</param>
        /// <param name="value">Value to bind to the variable.</param>
        /// <returns></returns>
        ISearchExpression SetValue(string name, object value);

        /// <summary>
        /// Assign a concrete provider in a SearchExpression.
        /// </summary>
        /// <param name="name">Name of the Provider </param>
        /// <param name="provider">Actual provider</param>
        /// <returns></returns>
        ISearchExpression SetProvider(string name, SearchProvider provider);
    }

    class SearchExpression : ISearchExpression
    {
        struct ExpressionField
        {
            public const string name = nameof(name);
            public const string type = nameof(type);
            public const string source = nameof(source);
            public const string value = nameof(value);
            public const string variables = nameof(variables);
            public const string properties = nameof(properties);

            // View model properties
            public const string position = nameof(position);
            public const string color = nameof(color);
        }

        private readonly SearchContext m_EmptyContext = new SearchContext(new SearchProvider[0], String.Empty);
        private SearchExpressionNode m_EvalNode;
        private Dictionary<string, SearchExpressionNode> m_Nodes;
        private HashSet<SearchItem> m_Items;
        private System.Diagnostics.Stopwatch m_Timer = new System.Diagnostics.Stopwatch();
        private SearchRequest m_CurrentRequest;
        private Dictionary<string, SearchProvider> m_AdditionalProviders = new Dictionary<string, SearchProvider>();
        private SearchFlags m_SearchOptions;

        public IEnumerable<SearchExpressionNode> nodes => m_Nodes.Values;
        public bool pending => m_CurrentRequest?.resolving ?? m_EmptyContext.searchInProgress;
        public SearchContext context => m_CurrentRequest?.context ?? m_EmptyContext;

        public int Count => m_Items.Count;
        public bool IsReadOnly => true;

        internal int requestCount { get; private set; }
        public double elapsedTime => Math.Round(m_Timer.Elapsed.TotalMilliseconds);

        public SearchExpression(SearchFlags options)
        {
            m_SearchOptions = options;
            Reset();
        }

        public IEnumerable<SearchItem> Fetch() { return m_Items; }
        public bool Contains(SearchItem item) { return m_Items.Contains(item); }
        public void CopyTo(SearchItem[] array, int arrayIndex) { m_Items.CopyTo(array, arrayIndex); }
        public IEnumerator<SearchItem> GetEnumerator() { return m_Items.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return m_Items.GetEnumerator(); }
        public IEnumerable<SearchItem> GetRange(int skipCount, int count) { throw new NotSupportedException(); }
        public void Clear() { throw new NotSupportedException(); }
        public void Add(SearchItem item) { throw new NotSupportedException(); }
        public void AddItems(IEnumerable<SearchItem> items) { throw new NotSupportedException(); }
        public void InsertRange(int index, IEnumerable<SearchItem> items) { throw new NotSupportedException(); }
        public bool Remove(SearchItem item) { throw new NotSupportedException(); }

        public void Dispose()
        {
            m_Items.Clear();
            m_Nodes.Clear();
        }

        public void Reset()
        {
            m_Items = new HashSet<SearchItem>();
            m_EvalNode = new SearchExpressionNode(ExpressionType.Results);
            m_Nodes = new Dictionary<string, SearchExpressionNode>() { { m_EvalNode.id, m_EvalNode } };
            requestCount = 0;
        }

        public ISearchList Evaluate()
        {
            if (m_EvalNode == null || m_EvalNode.type != ExpressionType.Results)
                throw new ExpressionException("Nothing to evaluate");

            if (m_EvalNode.source == null)
                return this;

            EvaluateNode(m_EvalNode.source);
            return this;
        }

        public ISearchExpression SetValue(string name, object value)
        {
            foreach (var n in nodes)
            {
                if (name.Equals(n.name))
                    n.value = value;
            }
            return this;
        }

        public ISearchExpression SetProvider(string name, SearchProvider provider)
        {
            m_AdditionalProviders[name] = provider;
            return this;
        }

        public SearchRequest EvaluateNode(SearchExpressionNode node)
        {
            if (node == null)
                throw new ExpressionException(m_EvalNode, $"Nothing to evaluate, node has no source");

            m_Items.Clear();
            m_Timer.Restart();
            requestCount = 0;

            m_CurrentRequest = BuildRequest(node);
            if (m_CurrentRequest != null)
                m_CurrentRequest.Resolve(OnSearchItemsReceived, OnEvaluationEnded);

            return m_CurrentRequest;
        }

        private void OnSearchItemsReceived(IEnumerable<SearchItem> results)
        {
            m_Items.UnionWith(results);
        }

        private void OnEvaluationEnded(SearchRequest exSearch)
        {
            m_Timer.Stop();
        }

        private SearchRequest BuildRequest(SearchExpressionNode node)
        {
            switch (node.type)
            {
                case ExpressionType.Results: return EvaluateNode(m_EvalNode.source);
                case ExpressionType.Search: return BuildSearchRequest(node);
                case ExpressionType.Select: return BuildSelectRequest(node);
                case ExpressionType.Union: return BuildUnionRequest(node);
                case ExpressionType.Intersect: return BuildIntersectRequest(node);
                case ExpressionType.Except: return BuildExceptRequest(node);
                
                case ExpressionType.Expression: 
                    if (node.source != null)
                        return BuildRequest(node.source);
                    return SearchRequest.empty;

                default:
                    throw new ExpressionException($"Cannot evaluate {node.id} of type {node.type}");
            }
        }

        private SearchRequest BuildSelectRequest(SearchExpressionNode node)
        {
            if (node.source == null)
                return SearchRequest.empty;

            var sourceRequest = BuildRequest(node.source);
            var selectField = node.selectField;
            var objectType = node.GetProperty<string>("type", null);
            var propertyName = node.GetProperty<string>("field", null);
            return sourceRequest.Select(selectField, objectType, propertyName);
        }

        private SearchRequest BuildFromSearchQuery(SearchExpressionNode node, string searchQuery)
        {
            var providers = new List<SearchProvider>();
            if (node.source?.type == ExpressionType.Provider)
            {
                var selectedProviderName = Convert.ToString(node.source.value);
                if (!m_AdditionalProviders.TryGetValue(selectedProviderName, out var selectedProvider))
                    selectedProvider = SearchService.GetProvider(selectedProviderName);
                if (selectedProvider != null)
                    providers.Add(selectedProvider);
            }
            else if (node.source == null)
                providers.AddRange(SearchService.Providers.Where(p => p.active));
            else
                throw new NotSupportedException($"Evaluation of source node {node.source.id} of type {node.source.type} is not supported.");

            requestCount++;
            return new SearchRequest(node.type, SearchService.CreateContext(providers, searchQuery, m_SearchOptions));
        }

        private SearchRequest BuildSearchRequest(SearchExpressionNode node)
        {
            List<ExpressionVariable> dynamicVariables = new List<ExpressionVariable>();
            var searchQuery = Convert.ToString(node.value);

            // Replace constants
            if (node.variables != null)
            {
                foreach (var v in node.variables)
                {
                    if (v.type != ExpressionType.Value)
                    {
                        if (v.source != null)
                            dynamicVariables.Add(v);
                        continue;
                    }
                
                    var constantValue = Convert.ToString(v.source.value);
                    if (String.IsNullOrEmpty(constantValue))
                        UnityEngine.Debug.LogWarning($"Constant value is null for {v.source.id}");
                
                    searchQuery = searchQuery.Replace($"${v.name}", constantValue);
                }
            }

            if (dynamicVariables.Count == 0)
                return BuildFromSearchQuery(node, searchQuery);
            
            var req = new SearchRequest(node.type);
            var varFetchResolved = new Dictionary<string, bool>();
            var varResults = new Dictionary<string, HashSet<string>>();

            foreach (var v in dynamicVariables)
                varFetchResolved[v.name] = false;

            foreach (var v in dynamicVariables)
            {
                var varName = v.name;
                var varRequest = BuildRequest(v.source);
                
                req.DependsOn(varRequest);
                varRequest.Resolve(items =>
                {
                    if (!varResults.TryGetValue(varName, out var results))
                    {
                        results = new HashSet<string>();
                        varResults[varName] = results;
                    }

                    results.UnionWith(items.Select(i => i.id));
                }, r =>
                {
                    varFetchResolved[varName] = true;

                    if (varFetchResolved.All(kvp => kvp.Value))
                    {
                        var totalRequest = 1;
                        foreach (var vrs in varResults)
                        {
                            if (vrs.Value.Count == 0)
                                continue;
                            totalRequest *= vrs.Value.Count;
                        }

                        var queries = new string[totalRequest];
                        for (int i = 0; i < totalRequest; ++i)
                            queries[i] = searchQuery;

                        foreach (var vrs in varResults)
                        {
                            if (vrs.Value.Count == 0)
                                continue;

                            for (int i = 0; i < totalRequest;)
                            {
                                foreach (var varValue in vrs.Value)
                                {
                                    queries[i] = queries[i].Replace($"${vrs.Key}", varValue);
                                    ++i;
                                }
                            }
                        }

                        requestCount+=totalRequest;
                        foreach (var q in queries)
                            BuildFromSearchQuery(node, q).TransferTo(req);
                    }
                });
            }

            return req;
        }

        private SearchRequest BuildUnionRequest(SearchExpressionNode ex)
        {
            if (ex.variables == null)
                return null;

            if (ex.variables == null)
                return null;

            var unionItems = new HashSet<SearchItem>();
            var unionRequest = new SearchRequest(ex.type);
            foreach (var v in ex.variables)
            {
                if (v.source != null)
                {
                    var sourceRequest = BuildRequest(v.source);
                    unionRequest.DependsOn(sourceRequest);

                    sourceRequest.Resolve(results => unionItems.UnionWith(results), null);
                }
            }

            unionRequest.resolved += exs => unionRequest.ProcessItems(exs.context, unionItems);
            return unionRequest;
        }

        private SearchRequest BuildIntersectRequest(SearchExpressionNode ex)
        {
            return BuildTwoSetRequest(ex, (sourceItems, withItems) => sourceItems.Intersect(withItems));
        }

        private SearchRequest BuildExceptRequest(SearchExpressionNode ex)
        {
            return BuildTwoSetRequest(ex, (sourceItems, withItems) => sourceItems.Except(withItems));
        }

        private SearchRequest BuildTwoSetRequest(SearchExpressionNode ex, Func<IList<SearchItem>, IList<SearchItem>, IEnumerable<SearchItem>> transformer)
        {
            if (ex.variables == null)
                return null;

            var exSearch = new SearchRequest(ex.type);
            if (ex.source != null && ex.TryGetVariableSource("With", out var withSource))
            {
                var sourceExpression = BuildRequest(ex.source);
                var withExpression = BuildRequest(withSource);

                exSearch.DependsOn(sourceExpression);
                exSearch.DependsOn(withExpression);

                bool fetchSourceItemsFinished = false;
                bool fetchWithItemsFinished = false;
                var sourceItems = new List<SearchItem>();
                var withItems = new List<SearchItem>();

                sourceExpression.Resolve(results => sourceItems.AddRange(results), exs => fetchSourceItemsFinished = true);
                withExpression.Resolve(results => withItems.AddRange(results), exs => fetchWithItemsFinished = true);

                exSearch.resolved += exs =>
                {
                    if (fetchSourceItemsFinished && fetchWithItemsFinished)
                        exSearch.ProcessItems(null, transformer(sourceItems, withItems));
                };
            }

            return exSearch;
        }

        public SearchExpressionNode AddNode(ExpressionType type)
        {
            var node = new SearchExpressionNode(type);
            m_Nodes.Add(node.id, node);
            return node;
        }

        public void RemoveNode(string id)
        {
            m_Nodes.Remove(id);
        }

        public SearchExpressionNode FromSource(SearchExpressionNode ex)
        {
            foreach (var n in nodes)
            {
                if (n.HasSource(ex))
                    return n;
            }
            return null;
        }

        public IEnumerable<TResult> Select<TResult>(Func<SearchItem, TResult> selector)
        {
            return m_Items.Select(item => selector(item));
        }

        public void Parse(string sjson)
        {
            Load((IDictionary)SJSON.LoadString(sjson));
        }

        public void Load(string path)
        {
            Load((IDictionary)SJSON.Load(path));
        }

        public void Load(IDictionary data)
        {
            m_Nodes.Clear();
            foreach (var kvp in data)
            {
                var id = (string)((DictionaryEntry)kvp).Key;
                var info = (IDictionary)((DictionaryEntry)kvp).Value;
                m_Nodes.Add(id, CreateNode(id, info));
            }

            foreach (var kvp in m_Nodes)
            {
                var node = kvp.Value;
                var info = (IDictionary)data[node.id];
                LoadNodeData(node, info);

                if (node.type == ExpressionType.Results)
                    m_EvalNode = node;
            }
        }

        private SearchExpressionNode ParseNode(IDictionary info)
        {
            var node = CreateNode(SearchExpressionNode.NewId(), info);
            return LoadNodeData(node, info);
        }

        private SearchExpressionNode CreateNode(string id, IDictionary info)
        {
            if (!info.Contains(ExpressionField.type))
                throw new ExpressionException($"Expression node {id} needs to have a type defined");

            if (m_Nodes.ContainsKey(id))
                throw new ExpressionException($"Expression node {id} already exists");

            var type = (string)info[ExpressionField.type];
            if (!Enum.TryParse<ExpressionType>(type, true, out var typeEnum))
                throw new ExpressionException($"Expression node {id} of type {type} is not supported");

            return new SearchExpressionNode(id, typeEnum);
        }

        private SearchExpressionNode LoadNodeData(SearchExpressionNode node, IDictionary info)
        {
            if (SJSON.TryGetValue(info, ExpressionField.name, out var name))
                node.name = Convert.ToString(name);

            if (SJSON.TryGetValue(info, ExpressionField.value, out var value))
                node.value = value;

            if (node.type == ExpressionType.Expression)
            {
                var nestedExpression = new SearchExpression(m_SearchOptions);

                if (node.value is string expressionPath && File.Exists(expressionPath))
                    nestedExpression.Load(expressionPath);
                else if (SJSON.TryGetValue(info, ExpressionField.source, out var source) && source is IDictionary sourceData)
                    nestedExpression.Load(sourceData);

                if (nestedExpression.m_EvalNode != null && nestedExpression.m_EvalNode.source != null)
                    node.source = nestedExpression.m_EvalNode.source;
            }
            else
            {
                if (SJSON.TryGetValue(info, ExpressionField.source, out var source))
                {
                    if (source is IDictionary nestedSource)
                        node.source = ParseNode(nestedSource);
                    else if (source is string sourceString && m_Nodes.TryGetValue(sourceString, out var sourceNode))
                        node.source = sourceNode;
                    else
                        node.value = source;
                }
            }

            if (SJSON.TryGetValue(info, ExpressionField.position, out var _obj) && _obj is object[] position && position.Length == 2)
                node.position = new Vector2((float)(double)position[0], (float)(double)position[1]);

            if (SJSON.TryGetValue(info, ExpressionField.color, out var _color) 
                && ColorUtility.TryParseHtmlString("#"+Convert.ToString(_color), out var color))
            {
                node.color = color;
            }

            if (SJSON.TryGetValue(info, ExpressionField.variables, out var variablesData))
            {
                var variables = (IDictionary)variablesData;
                foreach (var v in variables)
                {
                    var varName = (string)((DictionaryEntry)v).Key;
                    var valueSource = ((DictionaryEntry)v).Value;
                    if (valueSource == null)
                        node.AddVariable(varName);
                    else if (valueSource is IDictionary nestedSource)
                        node.AddVariable(varName, ParseNode(nestedSource));
                    else if (valueSource is string && m_Nodes.TryGetValue((string)valueSource, out var sourceNode))
                    {
                        node.AddVariable(varName, sourceNode);
                    }
                    else
                        throw new ExpressionException(node, $"Expression node {node.id} has an invalid variable {varName} with source {valueSource}");
                }
            }

            if (SJSON.TryGetValue(info, ExpressionField.properties, out var propertiesData))
            {
                var properties = (IDictionary)propertiesData;
                foreach (var v in properties)
                {
                    var propertyName = (string)((DictionaryEntry)v).Key;
                    var propertyValue = ((DictionaryEntry)v).Value;
                    node.SetProperty(propertyName, propertyValue);
                }
            }

            return node;
        }

        public void Save(string path)
        {
            SJSON.Save(Export(), path);
        }

        public IDictionary Export()
        {
            var expressionData = new Dictionary<string, object>();
            foreach (var node in m_Nodes.Values)
            {
                var nodeData = new Dictionary<string, object>() { { ExpressionField.type, node.type.ToString().ToLowerInvariant() } };

                if (node.name != null)
                    nodeData[ExpressionField.name] = node.name;

                if (node.source != null && nodes.Any(n => n.id == node.source.id))
                    nodeData[ExpressionField.source] = node.source.id;

                if (node.value != null)
                    nodeData[ExpressionField.value] = node.value;

                if (node.variables != null)
                    nodeData[ExpressionField.variables] = node.variables.ToDictionary(v => v.name, v => v.source?.id);

                if (node.properties != null)
                    nodeData[ExpressionField.properties] = node.properties;

                nodeData[ExpressionField.position] = new object[] { node.position.x, node.position.y };

                if (node.color != Color.clear && node.color != SearchExpressionNode.GetNodeTypeColor(node.type))
                    nodeData[ExpressionField.color] = ColorUtility.ToHtmlStringRGB(node.color);

                expressionData.Add(node.id, nodeData);
            }

            return expressionData;
        }
    }
}
