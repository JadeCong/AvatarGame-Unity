using UnityEngine;

namespace Unity.QuickSearch
{
    static class IndexerExtensions
    {
        [CustomObjectIndexer(typeof(Material))]
        internal static void MaterialShaderReferences(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            var material = context.target as Material;
            if (material == null)
                return;

            if (material.shader)
            {
                var fullShaderName = material.shader.name.ToLowerInvariant();
                var shortShaderName = System.IO.Path.GetFileNameWithoutExtension(fullShaderName);
                indexer.AddProperty("ref", shortShaderName, context.documentIndex, saveKeyword: false);
                indexer.AddProperty("ref", fullShaderName, context.documentIndex, saveKeyword: false);
            }
        }
    }
}
