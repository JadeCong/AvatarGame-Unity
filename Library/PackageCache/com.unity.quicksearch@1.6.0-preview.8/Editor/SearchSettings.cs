using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.QuickSearch
{
    internal enum SearchAssetIndexing
    {
        NoIndexing,
        BasicIndexing,
        FullIndexing
    }

    internal enum ProjectSize
    {
        Small,
        Medium,
        Large
    }

    internal class SearchProviderSettings : IDictionary
    {
        public bool active;
        public int priority;
        public string defaultAction;

        public int Count => 3;
        public ICollection Keys => new string[] { nameof(active), nameof(priority), nameof(defaultAction) };
        public ICollection Values => new object[] { active, priority, defaultAction };

        public SearchProviderSettings()
        {
            active = true;
            priority = 0;
            defaultAction = null;
        }

        public object this[object key]
        {
            get
            {
                switch ((string)key)
                {
                    case nameof(active): return active;
                    case nameof(priority): return priority;
                    case nameof(defaultAction): return defaultAction;
                }
                return null;
            }

            set => throw new NotSupportedException();
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            var d = new Dictionary<string, object>()
            {
                {nameof(active), active},
                {nameof(priority), priority},
                {nameof(defaultAction), defaultAction}
            };
            return d.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsFixedSize => true;
        public bool IsReadOnly => true;
        public bool IsSynchronized => true;
        public object SyncRoot => this;

        public void Add(object key, object value) { throw new NotSupportedException(); }
        public void Clear() { throw new NotSupportedException(); }
        public bool Contains(object key) { throw new NotSupportedException(); }
        public void CopyTo(Array array, int index) { throw new NotSupportedException(); }
        public void Remove(object key) { throw new NotSupportedException(); }
    }

    internal static class SearchSettings
    {
        internal const string k_RootIndexPath = "Assets/Assets.index";
        private const string k_ProjectUserSettingsPath = "UserSettings/QuickSearch.settings";

        public const string settingsPreferencesKey = "Preferences/Quick Search";
        public const string defaultQueryFolder = "Assets/Editor/Queries";

        internal static string k_AssetIndexPath = $"{Utils.packageFolderName}/Templates/Assets.index.template";

        // Per project settings
        public static bool trackSelection { get; private set; }
        public static bool fetchPreview { get; private set; }
        public static bool wantsMore { get; internal set; }
        public static string queryFolder { get; private set; }
        public static SearchAssetIndexing assetIndexing { get; internal set; }
        public static bool dockable { get; internal set; }
        public static bool debug { get; internal set; }
        public static float itemIconSize { get; internal set; }
        public static bool onBoardingDoNotAskAgain { get; internal set; }
        public static bool showPackageIndexes { get; internal set; }
        public static Dictionary<string, bool> filters { get; private set; }
        public static Dictionary<string, string> scopes { get; private set; }
        public static Dictionary<string, SearchProviderSettings> providers { get; private set; }

        static SearchSettings()
        {
            Load();
        }

        private static void Load()
        {
            if (!File.Exists(k_ProjectUserSettingsPath))
            {
                if (!Directory.Exists("UserSettings/"))
                    Directory.CreateDirectory("UserSettings/");
                File.WriteAllText(k_ProjectUserSettingsPath, "{}");
            }

            var settings = (IDictionary)SJSON.Load(k_ProjectUserSettingsPath);
            trackSelection = ReadSetting(settings, nameof(trackSelection), true);
            fetchPreview = ReadSetting(settings, nameof(fetchPreview), true);
            wantsMore = ReadSetting(settings, nameof(wantsMore), true);
            dockable = ReadSetting(settings, nameof(dockable), false);
            debug = ReadSetting(settings, nameof(debug), false);
            itemIconSize = ReadSetting(settings, nameof(itemIconSize), 1.0f);
            assetIndexing = (SearchAssetIndexing)ReadSetting(settings, nameof(assetIndexing), (int)SearchAssetIndexing.BasicIndexing);
            queryFolder = ReadSetting(settings, nameof(queryFolder), defaultQueryFolder);
            onBoardingDoNotAskAgain = ReadSetting(settings, nameof(onBoardingDoNotAskAgain), false);
            showPackageIndexes = ReadSetting(settings, nameof(showPackageIndexes), false);
            filters = ReadProperties<bool>(settings, nameof(filters));
            scopes = ReadProperties<string>(settings, nameof(scopes));
            providers = ReadProviderSettings(settings, nameof(providers));
        }

        internal static void Save()
        {
            var settings = new Dictionary<string, object>
            {
                [nameof(trackSelection)] = trackSelection,
                [nameof(fetchPreview)] = fetchPreview,
                [nameof(wantsMore)] = wantsMore,
                [nameof(dockable)] = dockable,
                [nameof(debug)] = debug,
                [nameof(itemIconSize)] = itemIconSize,
                [nameof(assetIndexing)] = (int)assetIndexing,
                [nameof(queryFolder)] = queryFolder,
                [nameof(onBoardingDoNotAskAgain)] = onBoardingDoNotAskAgain,
                [nameof(showPackageIndexes)] = showPackageIndexes,
                [nameof(filters)] = filters,
                [nameof(scopes)] = scopes,
                [nameof(providers)] = providers
            };

            SJSON.Save(settings, k_ProjectUserSettingsPath);
        }

        internal static void SetScopeValue(string prefix, int hash, string value)
        {
            SearchSettings.scopes[$"{prefix}.{hash:X8}"] = value;
        }

        internal static string GetScopeValue(string prefix, int hash, string defaultValue)
        {
            if (scopes.TryGetValue($"{prefix}.{hash:X8}", out var value))
                return value;
            return defaultValue;
        }

        public static SearchFlags GetContextOptions()
        {
            SearchFlags options = SearchFlags.Default;
            options &= ~SearchFlags.IndexingMask;
            if (assetIndexing == SearchAssetIndexing.BasicIndexing)
                options |= SearchFlags.BasicIndexing;
            else if (assetIndexing == SearchAssetIndexing.FullIndexing)
                options |= SearchFlags.FullIndexing;
            if (wantsMore)
                options |= SearchFlags.WantsMore;
            return options;
        }

        public static SearchFlags ApplyContextOptions(SearchFlags options)
        {
            if ((options & SearchFlags.IndexingMask) == 0)
            {
                options &= ~SearchFlags.IndexingMask;
                if (assetIndexing == SearchAssetIndexing.BasicIndexing)
                    options |= SearchFlags.BasicIndexing;
                else if (assetIndexing == SearchAssetIndexing.FullIndexing)
                    options |= SearchFlags.FullIndexing;
            }

            if (wantsMore)
                options |= SearchFlags.WantsMore;

            if (debug)
                options |= SearchFlags.Debug;

            return options;
        }

        public static void ApplyContextOptions(SearchContext context)
        {
            context.options = ApplyContextOptions(context.options);
        }

        [UsedImplicitly, SettingsProvider]
        internal static SettingsProvider CreateSearchSettings()
        {
            var settings = new SettingsProvider(settingsPreferencesKey, SettingsScope.User)
            {
                guiHandler = DrawSearchSettings,
                keywords = new[] { "quick", "omni", "search" },
            };
            return settings;
        }

        private static void DrawSearchSettings(string searchContext)
        {
            EditorGUIUtility.labelWidth = 350;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(10);
                    EditorGUI.BeginChangeCheck();
                    {
                        if (Utils.isDeveloperBuild || debug)
                        {
                            debug = EditorGUILayout.Toggle(Styles.debugContent, debug);
                        }

                        dockable = EditorGUILayout.Toggle(Styles.dockableContent, dockable);
                        trackSelection = EditorGUILayout.Toggle(Styles.trackSelectionContent, trackSelection);
                        fetchPreview = EditorGUILayout.Toggle(Styles.fetchPreviewContent, fetchPreview);
                        GUILayout.BeginHorizontal();
                        assetIndexing = (SearchAssetIndexing)EditorGUILayout.EnumPopup(Styles.assetIndexingLabel, assetIndexing, GUILayout.MaxWidth(475f));
                        if (assetIndexing == SearchAssetIndexing.FullIndexing)
                        {
                            if (!File.Exists(k_RootIndexPath))
                            {
                                GUILayout.Space(10);
                                if (GUILayout.Button(Styles.createRootIndexButtonContent, GUILayout.MaxWidth(100)))
                                    CreateRootIndex();
                            }
                            GUILayout.Space(10);
                            if (GUILayout.Button(Styles.openIndexManagerButtonContent, GUILayout.MaxWidth(130)))
                                IndexManager.OpenWindow();
                        }
                        GUILayout.EndHorizontal();

                        DrawQueryFolder();

                        GUILayout.Space(10);
                        DrawProviderSettings();
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        Save();
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private static T ReadSetting<T>(IDictionary settings, string key, T defaultValue = default)
        {
            try
            {
                if (SJSON.TryGetValue(settings, key, out var value))
                    return (T)value;
            }
            catch (Exception)
            {
                // Any error will return the default value.
            }

            return defaultValue;
        }

        private static float ReadSetting(IDictionary settings, string key, float defaultValue = 0)
        {
            return (float)ReadSetting(settings, key, (double)defaultValue);
        }

        private static int ReadSetting(IDictionary settings, string key, int defaultValue = 0)
        {
            return (int)ReadSetting(settings, key, (double)defaultValue);
        }

        private static Dictionary<string, SearchProviderSettings> ReadProviderSettings(IDictionary settings, string fieldName)
        {
            var properties = new Dictionary<string, SearchProviderSettings>();
            if (SJSON.TryGetValue(settings, fieldName, out var _data) && _data is IDictionary dataDict)
            {
                foreach (var p in dataDict)
                {
                    try
                    {
                        if (p is DictionaryEntry e && e.Value is IDictionary vdict)
                        {
                            properties[(string)e.Key] = new SearchProviderSettings()
                            {
                                active = Convert.ToBoolean(vdict[nameof(SearchProviderSettings.active)]),
                                priority = (int)(double)vdict[nameof(SearchProviderSettings.priority)],
                                defaultAction = vdict[nameof(SearchProviderSettings.defaultAction)] as string,
                            };
                        }
                    }
                    catch
                    {
                        // ignore copy
                    }
                }
            }
            return properties;
        }

        private static Dictionary<string, T> ReadProperties<T>(IDictionary settings, string fieldName)
        {
            var properties = new Dictionary<string, T>();
            if (SJSON.TryGetValue(settings, fieldName, out var _data) && _data is IDictionary dataDict)
            {
                foreach (var p in dataDict)
                {
                    try
                    {
                        if (p is DictionaryEntry e)
                            properties[(string)e.Key] = (T)e.Value;
                    }
                    catch
                    {
                        // ignore copy
                    }
                }
            }
            return properties;
        }

        private static void DrawQueryFolder()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Saved Queries", queryFolder, EditorStyles.textField);
            if (GUILayout.Button("Browse...", Styles.browseBtn))
            {
                var folderName = "Queries";
                var baseFolder = Application.dataPath;
                if (Directory.Exists(queryFolder) && baseFolder != Application.dataPath)
                {
                    baseFolder = Path.GetDirectoryName(queryFolder);
                    folderName = Path.GetFileName(queryFolder);
                }

                var result = EditorUtility.SaveFolderPanel("Queries", baseFolder, folderName);
                if (!string.IsNullOrEmpty(result))
                {
                    result = Utils.CleanPath(result);
                    if (Directory.Exists(result) && Utils.IsPathUnderProject(result))
                    {
                        queryFolder = Utils.GetPathUnderProject(result);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawProviderSettings()
        {
            EditorGUILayout.LabelField("Provider Settings", EditorStyles.largeLabel);
            foreach (var p in SearchService.OrderedProviders)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);

                var settings = GetProviderSettings(p.name.id);

                var wasActive = p.active;
                p.active = GUILayout.Toggle(wasActive, Styles.toggleActiveContent);
                if (p.active != wasActive)
                    settings.active = p.active;

                using (new EditorGUI.DisabledGroupScope(!p.active))
                {
                    GUILayout.Label(new GUIContent(p.name.displayName, $"{p.name.id} ({p.priority})"), GUILayout.Width(175));
                }

                if (!p.isExplicitProvider)
                {
                    if (GUILayout.Button(Styles.increasePriorityContent, Styles.priorityButton))
                        LowerProviderPriority(p);
                    if (GUILayout.Button(Styles.decreasePriorityContent, Styles.priorityButton))
                        UpperProviderPriority(p);
                }
                else
                {
                    GUILayoutUtility.GetRect(Styles.increasePriorityContent, Styles.priorityButton);
                    GUILayoutUtility.GetRect(Styles.increasePriorityContent, Styles.priorityButton);
                }

                GUILayout.Space(20);

                using (new EditorGUI.DisabledScope(p.actions.Count < 2))
                {
                    EditorGUI.BeginChangeCheck();
                    var items = p.actions.Select(a => new GUIContent(a.displayName, a.content.image,
                        p.actions.Count == 1 ?
                        $"Default action for {p.name.displayName} (Enter)" :
                        $"Set default action for {p.name.displayName} (Enter)")).ToArray();
                    var newDefaultAction = EditorGUILayout.Popup(0, items, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetDefaultAction(p.name.id, p.actions[newDefaultAction].id);
                        GUI.changed = true;
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            if (GUILayout.Button(Styles.resetDefaultsContent, GUILayout.MaxWidth(170)))
                ResetProviderSettings();
            GUILayout.EndHorizontal();
        }

        internal static SearchProviderSettings GetProviderSettings(string providerId)
        {
            if (TryGetProviderSettings(providerId, out var settings))
                return settings;

            var provider = SearchService.GetProvider(providerId);
            if (provider == null)
                return new SearchProviderSettings();

            providers[providerId] = new SearchProviderSettings() { active = provider.active, priority = provider.priority, defaultAction = null };
            return providers[providerId];
        }

        internal static bool TryGetProviderSettings(string providerId, out SearchProviderSettings settings)
        {
            return providers.TryGetValue(providerId, out settings);
        }

        private static void ResetProviderSettings()
        {
            providers.Clear();
            SearchService.Refresh();
        }

        private static void LowerProviderPriority(SearchProvider provider)
        {
            var sortedProviderList = SearchService.Providers.Where(p => !p.isExplicitProvider).OrderBy(p => p.priority).ToList();
            for (int i = 1, end = sortedProviderList.Count; i < end; ++i)
            {
                var cp = sortedProviderList[i];
                if (cp != provider)
                    continue;

                var adj = sortedProviderList[i - 1];
                var temp = provider.priority;
                if (cp.priority == adj.priority)
                    temp++;

                provider.priority = adj.priority;
                adj.priority = temp;

                GetProviderSettings(adj.name.id).priority = adj.priority;
                GetProviderSettings(provider.name.id).priority = provider.priority;
                break;
            }
        }

        private static void UpperProviderPriority(SearchProvider provider)
        {
            var sortedProviderList = SearchService.Providers.Where(p => !p.isExplicitProvider).OrderBy(p => p.priority).ToList();
            for (int i = 0, end = sortedProviderList.Count - 1; i < end; ++i)
            {
                var cp = sortedProviderList[i];
                if (cp != provider)
                    continue;

                var adj = sortedProviderList[i + 1];
                var temp = provider.priority;
                if (cp.priority == adj.priority)
                    temp--;

                provider.priority = adj.priority;
                adj.priority = temp;

                GetProviderSettings(adj.name.id).priority = adj.priority;
                GetProviderSettings(provider.name.id).priority = provider.priority;
                break;
            }
        }

        private static void SetDefaultAction(string providerId, string actionId)
        {
            if (string.IsNullOrEmpty(providerId) || string.IsNullOrEmpty(actionId))
                return;

            GetProviderSettings(providerId).defaultAction = actionId;
            SortActionsPriority();
        }

        internal static void SortActionsPriority()
        {
            foreach (var searchProvider in SearchService.Providers)
                SortActionsPriority(searchProvider);
        }

        private static void SortActionsPriority(SearchProvider searchProvider)
        {
            if (searchProvider.actions.Count == 1)
                return;

            var defaultActionId = GetProviderSettings(searchProvider.name.id).defaultAction;
            if (string.IsNullOrEmpty(defaultActionId))
                return;
            if (searchProvider.actions.Count == 0 || defaultActionId == searchProvider.actions[0].id)
                return;

            searchProvider.actions.Sort((action1, action2) =>
            {
                if (action1.id == defaultActionId)
                    return -1;

                if (action2.id == defaultActionId)
                    return 1;

                return 0;
            });
        }

        private static void CreateRootIndex()
        {
            File.Copy(k_AssetIndexPath, k_RootIndexPath);
            AssetDatabase.ImportAsset(k_RootIndexPath, ImportAssetOptions.ForceSynchronousImport);
        }

        internal static void SetSettingsFromProjectSize(bool newFetchPreview, bool newTrackSelection, bool newWantsMore, SearchAssetIndexing newAssetIndexing)
        {
            fetchPreview = newFetchPreview;
            trackSelection = newTrackSelection;
            wantsMore = newWantsMore;
            assetIndexing = newAssetIndexing;
            Save();
        }

        static class Styles
        {
            public static GUIStyle priorityButton = new GUIStyle("Button")
            {
                fixedHeight = 20,
                fixedWidth = 20,
                fontSize = 14,
                padding = new RectOffset(0, 0, 0, 4),
                margin = new RectOffset(1, 1, 1, 1),
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            public static GUIStyle browseBtn = new GUIStyle("Button")
            {
                fixedWidth = 70
            };

            public static GUIStyle textFiedl = new GUIStyle(EditorStyles.textField);

            public static GUIContent toggleActiveContent = new GUIContent("", "Enable or disable this provider. Disabled search provider will be completely ignored by the search service.");
            public static GUIContent resetDefaultsContent = new GUIContent("Reset Providers Settings", "All search providers will restore their initial preferences (priority, active, default action)");
            public static GUIContent increasePriorityContent = new GUIContent("\u2191", "Increase the provider's priority");
            public static GUIContent decreasePriorityContent = new GUIContent("\u2193", "Decrease the provider's priority");

            public static GUIContent useDockableWindowContent = new GUIContent("Use a dockable window (instead of a modal popup window, not recommended)");
            public static GUIContent closeWindowByDefaultContent = new GUIContent("Automatically close the window when an action is executed");
            public static GUIContent useFilePathIndexerContent = new GUIContent(
                "Enable fast indexing of file system entries under your project (experimental)",
                "This indexing system takes around 1 and 10 seconds to build the first time you launch the quick search window. " +
                "It can take up to 30-40 mb of memory, but it provides very fast search for large projects. " +
                "Note that if you want to use standard asset database filtering, you will need to rely on `t:`, `a:`, etc.");
            public static GUIContent trackSelectionContent = new GUIContent(
                "Track the current selection in the quick search",
                "Tracking the current selection can alter other window state, such as pinging the project browser or the scene hierarchy window.");
            public static GUIContent fetchPreviewContent = new GUIContent(
                "Generate an asset preview thumbnail for found items",
                "Fetching the preview of the items can consume more memory and make searches within very large project slower.");
            public static GUIContent dockableContent = new GUIContent("Open Quick Search as dockable window");
            public static GUIContent debugContent = new GUIContent("[DEV] Display additional debugging information");
            public static GUIContent assetIndexingLabel = new GUIContent("Asset indexing mode","");
            public static GUIContent createRootIndexButtonContent = new GUIContent("Create index");
            public static GUIContent openIndexManagerButtonContent = new GUIContent("Index Manager");
        }
    }
}