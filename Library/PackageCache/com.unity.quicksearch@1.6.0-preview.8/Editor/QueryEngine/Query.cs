using System.Collections.Generic;

namespace Unity.QuickSearch
{
    internal interface IQueryHandler<out TData, in TPayload>
    {
        IEnumerable<TData> Eval(TPayload payload);
    }

    internal interface IQueryHandlerFactory<TData, out TQueryHandler, TPayload>
        where TQueryHandler : IQueryHandler<TData, TPayload>
    {
        TQueryHandler Create(QueryGraph graph, ICollection<QueryError> errors);
    }

    /// <summary>
    /// A Query defines an operation that can be used to filter a data set.
    /// </summary>
    /// <typeparam name="TData">The filtered data type.</typeparam>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    public class Query<TData, TPayload>
        where TPayload : class
    {
        /// <summary> Indicates if the query is valid or not. </summary>
        public bool valid => errors.Count == 0 && graph != null;

        /// <summary> List of QueryErrors. </summary>
        public ICollection<QueryError> errors { get; }

        internal IQueryHandler<TData, TPayload> graphHandler { get; set; }

        internal QueryGraph graph { get; }

        internal Query(QueryGraph graph, ICollection<QueryError> errors)
        {
            this.graph = graph;
            this.errors = errors;
        }

        internal Query(QueryGraph graph, ICollection<QueryError> errors, IQueryHandler<TData, TPayload> graphHandler)
            : this(graph, errors)
        {
            if (valid)
            {
                this.graphHandler = graphHandler;
            }
        }

        /// <summary>
        /// Apply the filtering on a payload.
        /// </summary>
        /// <param name="payload">The data to filter</param>
        /// <returns>A filtered IEnumerable.</returns>
        public virtual IEnumerable<TData> Apply(TPayload payload = null)
        {
            if (!valid)
                return null;
            return graphHandler.Eval(payload);
        }

        /// <summary>
        /// Optimize the query by optimizing the underlying filtering graph.
        /// </summary>
        /// <param name="propagateNotToLeaves">Propagate "Not" operations to leaves, so only leaves can have "Not" operations as parents.</param>
        /// <param name="swapNotToRightHandSide">Swaps "Not" operations to the right hand side of combining operations (i.e. "And", "Or"). Useful if a "Not" operation is slow.</param>
        public void Optimize(bool propagateNotToLeaves, bool swapNotToRightHandSide)
        {
            graph?.Optimize(propagateNotToLeaves, swapNotToRightHandSide);
        }
    }

    /// <summary>
    /// A Query defines an operation that can be used to filter a data set.
    /// </summary>
    /// <typeparam name="T">The filtered data type.</typeparam>
    public class Query<T> : Query<T, IEnumerable<T>>
    {
        internal Query(QueryGraph graph, ICollection<QueryError> errors, IQueryHandler<T, IEnumerable<T>> graphHandler)
            : base(graph, errors, graphHandler)
        { }

        /// <summary>
        /// Apply the filtering on an IEnumerable data set.
        /// </summary>
        /// <param name="data">The data to filter</param>
        /// <returns>A filtered IEnumerable.</returns>
        public override IEnumerable<T> Apply(IEnumerable<T> data)
        {
            if (!valid)
                return new T[] { };
            return graphHandler.Eval(data);
        }
    }
}