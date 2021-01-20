using System.Collections.Generic;

namespace Unity.QuickSearch
{
    internal enum QueryNodeType
    {
        And,
        Or,
        Filter,
        Search,
        Not,
        NoOp,
        NestedQuery,
        Where,
        In,
        Union,
        Intersection,
        Aggregator
    }

    internal interface IQueryNode
    {
        IQueryNode parent { get; set; }
        QueryNodeType type { get; }
        List<IQueryNode> children { get; }
        bool leaf { get; }
        string identifier { get; }
        int QueryHashCode();
        int queryStringPosition { get; set; }
    }

    internal class FilterNode : IQueryNode
    {
        public IFilter filter;

        public IQueryNode parent { get; set; }
        public virtual QueryNodeType type => QueryNodeType.Filter;
        public virtual List<IQueryNode> children => null;
        public virtual bool leaf => true;
        public string identifier { get; }
        public int queryStringPosition { get; set; }

        public FilterOperator op;
        public string filterValue;
        public string paramValue;

        public FilterNode(IFilter filter, FilterOperator op, string filterValue, string paramValue, string filterString)
        {
            this.filter = filter;
            identifier = filterString;
            this.op = op;
            this.filterValue = filterValue;
            this.paramValue = paramValue;
        }

        public int QueryHashCode()
        {
            return identifier.GetHashCode();
        }
    }

    internal class SearchNode : IQueryNode
    {
        public bool exact { get; }
        public string searchValue { get; }

        public IQueryNode parent { get; set; }
        public QueryNodeType type => QueryNodeType.Search;
        public List<IQueryNode> children => null;
        public bool leaf => true;
        public string identifier { get; private set; }
        public int queryStringPosition { get; set; }

        public SearchNode(string searchValue, bool isExact)
        {
            this.searchValue = searchValue;
            exact = isExact;
            identifier = exact ? ("!" + searchValue) : searchValue;
        }

        public int QueryHashCode()
        {
            return identifier.GetHashCode();
        }
    }

    internal abstract class CombinedNode : IQueryNode
    {
        public IQueryNode parent { get; set; }
        public abstract QueryNodeType type { get; }
        public List<IQueryNode> children { get; }
        public bool leaf => children.Count == 0;
        public abstract string identifier { get; }
        public int queryStringPosition { get; set; }

        protected CombinedNode()
        {
            children = new List<IQueryNode>();
        }

        public void AddNode(IQueryNode node)
        {
            children.Add(node);
            node.parent = this;
        }

        public void RemoveNode(IQueryNode node)
        {
            if (!children.Contains(node))
                return;

            children.Remove(node);
            if (node.parent == this)
                node.parent = null;
        }

        public void Clear()
        {
            foreach (var child in children)
            {
                if (child.parent == this)
                    child.parent = null;
            }
            children.Clear();
        }

        public abstract void SwapChildNodes();

        public virtual int QueryHashCode()
        {
            var hc = 0;
            foreach (var child in children)
            {
                hc ^= child.GetHashCode();
            }
            return hc;
        }
    }

    internal class AndNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.And;
        public override string identifier => "(" + children[0].identifier + " " + children[1].identifier + ")";

        public override void SwapChildNodes()
        {
            if (children.Count != 2)
                return;

            var tmp = children[0];
            children[0] = children[1];
            children[1] = tmp;
        }
    }

    internal class OrNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.Or;
        public override string identifier => "(" + children[0].identifier + " or " + children[1].identifier + ")";

        public override void SwapChildNodes()
        {
            if (children.Count != 2)
                return;

            var tmp = children[0];
            children[0] = children[1];
            children[1] = tmp;
        }
    }

    internal class NotNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.Not;
        public override string identifier => "-" + children[0].identifier;

        public override void SwapChildNodes()
        { }
    }

    internal class NestedQueryNode : IQueryNode
    {
        public IQueryNode parent { get; set; }
        public QueryNodeType type => QueryNodeType.NestedQuery;
        public List<IQueryNode> children { get; } = new List<IQueryNode>();
        public bool leaf => true;
        public string identifier { get; }
        public string associatedFilter { get; }
        public int queryStringPosition { get; set; }

        public INestedQueryHandler nestedQueryHandler { get; }

        public NestedQueryNode(string nestedQuery, INestedQueryHandler nestedQueryHandler)
        {
            identifier = nestedQuery;
            this.nestedQueryHandler = nestedQueryHandler;
        }

        public NestedQueryNode(string nestedQuery, string filterToken, INestedQueryHandler nestedQueryHandler)
            : this(nestedQuery, nestedQueryHandler)
        {
            associatedFilter = filterToken;
        }

        public int QueryHashCode()
        {
            return identifier.GetHashCode();
        }
    }

    internal class WhereNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.Where;
        public override string identifier => children[0].identifier;

        public WhereNode()
        { }

        public override void SwapChildNodes()
        { }
    }

    internal class InFilterNode : FilterNode
    {
        public override QueryNodeType type => QueryNodeType.In;
        public override List<IQueryNode> children { get; } = new List<IQueryNode>();
        public override bool leaf => children.Count == 0;

        public InFilterNode(IFilter filter, FilterOperator op, string filterValue, string paramValue, string filterString)
            : base(filter, op, filterValue, paramValue, filterString) { }
    }

    internal class UnionNode : OrNode
    {
        public override QueryNodeType type => QueryNodeType.Union;
    }

    internal class IntersectionNode : AndNode
    {
        public override QueryNodeType type => QueryNodeType.Intersection;
    }

    internal class AggregatorNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.Aggregator;
        public override string identifier { get; }
        public INestedQueryAggregator aggregator { get; }

        public AggregatorNode(string token, INestedQueryAggregator aggregator)
        {
            identifier = token;
            this.aggregator = aggregator;
        }

        public override void SwapChildNodes()
        { }
    }

    internal sealed class NoOpNode : IQueryNode
    {
        public IQueryNode parent { get; set; }
        public QueryNodeType type => QueryNodeType.NoOp;
        public List<IQueryNode> children => new List<IQueryNode>();
        public bool leaf => true;
        public string identifier { get; }
        public int queryStringPosition { get; set; }

        public NoOpNode(string identifier)
        {
            this.identifier = identifier;
        }

        public int QueryHashCode()
        {
            return identifier.GetHashCode();
        }
    }
}
