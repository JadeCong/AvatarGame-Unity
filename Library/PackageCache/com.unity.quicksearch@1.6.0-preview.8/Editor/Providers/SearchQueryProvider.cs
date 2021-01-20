using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;

namespace Unity.QuickSearch
{
namespace Providers
{
    [UsedImplicitly]
    static class Query
    {
        internal const string type = "query";
        private const string displayName = "Queries";
        static List<SearchItem> m_SearchQueryItems;

        [UsedImplicitly, SearchItemProvider]
        private static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                filterId = "q:",
                isExplicitProvider = true,
                fetchItems = (context, items, provider) =>
                {
                    var queryItems = SearchQuery.GetAllSearchQueryItems();
                    if (string.IsNullOrEmpty(context.searchQuery))
                    {
                        items.AddRange(queryItems);
                    }
                    else
                    {
                        foreach (var qi in queryItems)
                        {
                            if (SearchUtils.MatchSearchGroups(context, qi.label, true) ||
                                SearchUtils.MatchSearchGroups(context, ((SearchQuery)qi.data).searchQuery, true))
                            {
                                items.Add(qi);
                            }
                        }
                    }
                    return null;
                }
            };
        }

        [UsedImplicitly, SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "exec", null, "Execute search query")
                {
                    closeWindowAfterExecution = false,
                    handler = (item) => SearchQuery.ExecuteQuery(item?.context.searchView, (SearchQuery)item.data)
                },
                new SearchAction(type, "select", null, "Select search query")
                {
                    handler = (item) => Utils.FrameAssetFromPath(item.id)
                }
            };
        }
    }
}
}
