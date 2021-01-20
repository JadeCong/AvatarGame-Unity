using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Unity.QuickSearch
{
    enum ExpressionSelectField
    {
        Default,
        Type,
        Object,
        Asset = Object,
        Component
    }

    [DebuggerDisplay("{name} ({source})")]
    class ExpressionVariable : IEquatable<ExpressionVariable>
    {
        public string name;
        public SearchExpressionNode source;

        public ExpressionVariable(string name, SearchExpressionNode source = null)
        {
            this.name = name;
            this.source = source;
        }

        public ExpressionType type
        {
            get
            {
                if (source == null)
                    return ExpressionType.Undefined;
                return source.type;
            }
        }

        public bool Equals(ExpressionVariable other)
        {
            return other.name == name;
        }
    }

    [DebuggerDisplay("{id} ({value})")]
    class SearchExpressionNode
    {
        public string id { get; private set; }
        public ExpressionType type { get; private set; }
        public string name { get; set; }
        public object value { get; set; }
        public Vector2 position { get; set; }
        public Color color { get; set; }
        public SearchExpressionNode source { get; internal set; }
        public List<ExpressionVariable> variables { get; internal set; }
        public Dictionary<string, object> properties { get; private set; }

        internal static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public ExpressionSelectField selectField
        {
            get
            {
                if (Enum.TryParse<ExpressionSelectField>(value as string, true, out var type))
                    return type;
                return ExpressionSelectField.Default;
            }
        }

        public SearchExpressionNode(ExpressionType type)
            : this(NewId(), type)
        {
        }

        public SearchExpressionNode(ExpressionType type, SearchExpressionNode source, object value)
            : this(NewId(), type)
        {
            this.source = source;
            this.value = value;
        }

        public SearchExpressionNode(string id, ExpressionType type)
        {
            this.id = id;
            this.type = type;

            color = GetNodeTypeColor(type);
        }

        internal static Color GetNodeTypeColor(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Provider: return new Color(80/255f, 99/255f, 93/255f, 0.99f);
                case ExpressionType.Value: return new Color(35/255f, 35/255f, 35/255f, 0.79f);
                case ExpressionType.Search: return new Color(20/255f, 87/255f, 132/255f, 0.99f);
                case ExpressionType.Select: return new Color(18/255f, 57/255f, 126/255f, 0.99f);
                case ExpressionType.Union: return new Color(160/255f, 99/255f, 31/255f, 0.99f);
                case ExpressionType.Intersect: return new Color(120/255f, 99/255f, 33/255f, 0.99f);
                case ExpressionType.Except: return new Color(142/255f, 34/255f, 10/255f, 0.99f);
                case ExpressionType.Results: return new Color(9/255f, 99/255f, 9/255f, 0.99f);
                case ExpressionType.Expression: return new Color(75/255f, 111/255f, 75/255f, 0.99f);
            }

            return Color.clear;
        }

        public bool HasVariable(string name)
        {
            if (variables == null)
                return false;
            if (variables.Any(v => v.name == name))
                return true;
            return false;
        }

        public bool RenameVariable(string currentName, string newName)
        {
            if (variables == null)
                return false;

            if (HasVariable(newName))
                return false;

            foreach (var v in variables)
            {
                if (v.name == currentName)
                {
                    v.name = newName;
                    return true;
                }
            }
            
            return false;
        }

        public bool TryGetVariableSource(string name, out SearchExpressionNode source)
        {
            source = null;
            foreach (var v in variables)
            {
                if (v.name == name)
                {
                    source = v.source;
                    return true;
                }
            }

            return false;
        }

        private void InitializeVariables()
        {
            if (variables == null)
                variables = new List<ExpressionVariable>();
        }

        public void SetVariableSource(string name, SearchExpressionNode source)
        {
            InitializeVariables();

            foreach (var v in variables)
            {
                if (v.name == name)
                {
                    v.source = source;
                    break;
                }
            }

            if (source != null)
                AddVariable(name, source);
        }

        public ExpressionVariable AddVariable(string name, SearchExpressionNode source = null)
        {
            InitializeVariables();

            foreach (var v in variables)
            {
                if (v.name == name)
                {
                    v.source = source;
                    return v;
                }
            }

            var newVar = new ExpressionVariable(name, source);
            variables.Add(newVar);
            return newVar;
        }

        public int RemoveVariable(string name)
        {
            return variables.RemoveAll(v => v.name == name);
        }

        internal bool HasSource(SearchExpressionNode ex)
        {
            if (source == ex)
                return true;

            if (variables == null)
                return false;
            
            foreach (var v in variables)
            {
                if (v.source == ex)
                    return true;
            }

            return false;
        }

        public int GetVariableCount()
        {
            return variables?.Count ?? 0;
        }

        public void SetProperty(string propertyName, object propertyValue)
        {
            if (properties == null)
                properties = new Dictionary<string, object>();
            properties[propertyName] = propertyValue;
        }

        public T GetProperty<T>(string propertyName, T defaultValue = default)
        {
            if (properties == null)
                return defaultValue;
            if (properties.TryGetValue(propertyName, out var value))
                return (T)value;
            return defaultValue;
        }
    }
}
