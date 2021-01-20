using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unity.QuickSearch.Providers
{
    class FileSearchIndexer : SearchIndexer, IDisposable
    {
        public string type { get; }

        public FileSearchIndexer(string type, IEnumerable<SearchIndexerRoot> roots)
            : base (roots)
        {
            this.type = type;
            skipEntryHandler = ShouldSkipEntry;
            getIndexFilePathHandler = GetIndexFilePath;
            getEntryComponentsHandler = (e, i) => SearchUtils.SplitFileEntryComponents(e, entrySeparators);
            enumerateRootEntriesHandler = EnumerateAssetPaths;

            AssetPostprocessorIndexer.contentRefreshed += UpdateIndexWithNewContent;
        }

        private static bool ShouldSkipEntry(string entry)
        {
            return entry.Length == 0 || entry[0] == '.' || entry.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase);
        }

        private string GetIndexFilePath(string basePath)
        {
            string indexFileName = $"quicksearch.{type}.index";
            return Path.GetFullPath(Path.Combine(basePath, "..", "Library", indexFileName));
        }

        private static IEnumerable<string> EnumerateAssetPaths(SearchIndexerRoot root)
        {
            var dirInfo = new DirectoryInfo(root.basePath);
            var hiddenFolders = dirInfo.GetDirectories("*", SearchOption.AllDirectories)
                .Where(d => (d.Attributes & FileAttributes.Hidden) != 0)
                .Select(d => d.FullName.Replace("\\", "/")).ToArray();

            return Directory.EnumerateFiles(root.basePath, "*.*", SearchOption.AllDirectories)
                .Select(entry => entry.Replace("\\", "/"))
                .Where(entry => !hiddenFolders.Any(d => entry.StartsWith(d, StringComparison.Ordinal)));
        }

        public void Dispose()
        {
            AssetPostprocessorIndexer.contentRefreshed -= UpdateIndexWithNewContent;
        }
    }
}
