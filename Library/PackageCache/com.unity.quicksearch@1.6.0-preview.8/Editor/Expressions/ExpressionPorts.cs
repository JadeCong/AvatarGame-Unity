using UnityEditor.Experimental.GraphView;

namespace Unity.QuickSearch
{
    class ExpressionPort {}
    class ExpressionSet : ExpressionPort {}
    class ExpressionSource : ExpressionPort {}
    class ExpressionResults : ExpressionSource {}
    class ExpressionConstant : ExpressionPort {}
    class ExpressionProvider : ExpressionResults {}

    static class NodeAdapterExtension
    {
        internal static bool Adapt(this NodeAdapter v, PortSource<ExpressionSet> a, PortSource<ExpressionResults> b) { return true; }
        internal static bool Adapt(this NodeAdapter v, PortSource<ExpressionResults> a, PortSource<ExpressionSet> b) { return true; }
        internal static bool Adapt(this NodeAdapter v, PortSource<ExpressionSet> a, PortSource<ExpressionVariable> b) { return true; }
        internal static bool Adapt(this NodeAdapter v, PortSource<ExpressionVariable> a, PortSource<ExpressionSet> b) { return true; }
        internal static bool Adapt(this NodeAdapter v, PortSource<ExpressionConstant> a, PortSource<ExpressionVariable> b) { return true; }
        internal static bool Adapt(this NodeAdapter v, PortSource<ExpressionVariable> a, PortSource<ExpressionConstant> b) { return true; }
        internal static bool Adapt(this NodeAdapter v, PortSource<ExpressionProvider> a, PortSource<ExpressionSource> b) { return true; }
        internal static bool Adapt(this NodeAdapter v, PortSource<ExpressionSource> a, PortSource<ExpressionProvider> b) { return true; }
    }
}