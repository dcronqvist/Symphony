using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Symphony;

internal class ContentCollection
{
    // Every content entry can consist of multiple items, so we need to store these efficiently.

    // From content item identifier to entry.
    private Dictionary<string, ContentEntry> _entries = new Dictionary<string, ContentEntry>();

    // From entry to items
    private Dictionary<ContentEntry, Dictionary<string, ContentItem>> _items = new Dictionary<ContentEntry, Dictionary<string, ContentItem>>();

    public ContentCollection()
    {
    }

    public ContentCollection(IEnumerable<(ContentEntry, ContentItem[])> entries)
    {
        foreach (var (entry, items) in entries)
        {
            foreach (var item in items)
            {
                this.AddItem(entry, item);
            }
        }
    }

    public bool HasItem(string identifier)
    {
        var entry = this._entries.GetValueOrDefault(identifier);
        return entry != null;
    }

    public IEnumerable<ContentEntry> GetEntriesWhere(Func<ContentEntry, bool> predicate)
    {
        return this._items.Keys.Where(predicate);
    }

    public ContentEntry GetEntryForItem(string identifier)
    {
        var entry = this._entries.GetValueOrDefault(identifier);
        if (entry == null)
        {
            throw new KeyNotFoundException($"No entry found for item {identifier}");
        }
        return entry;
    }

    public void AddItem(ContentEntry entry, ContentItem item)
    {
        this._entries.Add(item.Identifier!, entry);

        if (!this._items.ContainsKey(entry))
        {
            this._items.Add(entry, new());
        }

        this._items[entry].Add(item.Identifier!, item);
    }

    public void RemoveItem(string identifier)
    {
        var entry = this._entries.GetValueOrDefault(identifier);
        if (entry == null)
            return;
        this._entries.Remove(identifier);
        this._items.Remove(entry);
    }

    public ContentItem? GetContentItem(string identifier)
    {
        var entry = this._entries.GetValueOrDefault(identifier);
        if (entry == null)
            return null;

        if (this._items.TryGetValue(entry, out var itemDict))
        {
            return itemDict[identifier];
        }
        else
        {
            return null;
        }
    }

    public T? GetContentItem<T>(string identifier) where T : IContent
    {
        var entry = this._entries.GetValueOrDefault(identifier);
        if (entry == null)
            return default;

        if (this._items.TryGetValue(entry, out var itemDict))
        {
            return (T)itemDict[identifier].Content;
        }
        else
        {
            return default;
        }
    }

    public void ReplaceContentItem(string identifier, ContentItem item)
    {
        var entry = this._entries.GetValueOrDefault(identifier);
        if (entry == null)
            return;
        this._items[entry][identifier] = item;
    }

    public ContentCollection GetCopy()
    {
        var copy = new ContentCollection();
        foreach (var item in _items)
        {
            foreach (var (k, v) in item.Value)
            {
                copy.AddItem(item.Key, v);
            }
        }
        return copy;
    }

    public IEnumerable<ContentItem> GetItems()
    {
        return _items.Values.Select(x => x.Values).SelectMany(x => x);
    }

    public IEnumerable<(ContentEntry, ContentItem[])> GetEntriesAndItems()
    {
        return _items.Select(x => (x.Key, x.Value.Values.ToArray()));
    }
}

public class ContentManager<TMeta>
{
    private readonly IContentStructureValidator<TMeta> _structureValidator;
    private readonly IEnumerable<IContentSource> _sources;
    private readonly IContentLoader _loader;
    private readonly IContentOverwriter _overwriter;
    private readonly bool _hotReload;

    private ContentCollection _loadedContent = new();

    public ContentManager(IContentStructureValidator<TMeta> structureValidator, IEnumerable<IContentSource> sources, IContentLoader loader, IContentOverwriter overwriter, bool hotReload)
    {
        this._structureValidator = structureValidator;
        this._sources = sources;
        this._loader = loader;
        this._overwriter = overwriter;
        this._hotReload = hotReload;
    }

    private IEnumerable<IContentSource> CollectSourcesWithValidStructures()
    {
        foreach (var source in this._sources)
        {
            var structure = source.GetStructure();

            if (!_structureValidator.TryValidateStructure(structure, out TMeta? meta, out string? validationError))
                continue;

            yield return source;
        }
    }

    private async Task<ContentCollection> RunStageAsync(IEnumerable<(IContentSource firstSource, IContentSource lastSource, ContentEntry lastSourceEntry)> allEntries, IContentLoadingStage stage, ContentCollection previousLoaded)
    {
        var loaded = previousLoaded.GetCopy();
        var allEntriesGroupedByLastSource = allEntries.GroupBy(x => x.lastSource);

        foreach (var grouping in allEntriesGroupedByLastSource)
        {
            var groupingLastSource = grouping.Key;
            var lastSourceIdentifier = this._loader.GetIdentifierForSource(groupingLastSource);
            var firstSourceIdentifier = this._loader.GetIdentifierForSource(grouping.First().firstSource);

            using var lastSourceStructure = groupingLastSource.GetStructure();

            var affectedEntries = grouping.Where(x => stage.IsEntryAffectedByStage(x.lastSourceEntry.EntryPath)).ToList();

            foreach (var affectedEntry in affectedEntries)
            {
                IContentSource firstSource = affectedEntry.firstSource;
                IContentSource lastSource = affectedEntry.lastSource;
                ContentEntry lastSourceEntry = affectedEntry.lastSourceEntry;

                lastSourceEntry.LastWriteTime = lastSourceStructure.GetLastWriteTimeForEntry(lastSourceEntry.EntryPath);

                var entryLoadResults = await Task.Run(() => stage.LoadEntry(lastSourceEntry, lastSourceStructure.GetEntryStream(lastSourceEntry.EntryPath)));

                await foreach (var loadResult in entryLoadResults)
                {
                    if (!loadResult.Success)
                        continue;

                    var content = loadResult.Content!;
                    var itemIdentifier = $"{firstSourceIdentifier}:{loadResult.ItemIdentifier}";

                    var newContentItem = new ContentItem(itemIdentifier, firstSource, lastSource, content);
                    loaded.AddItem(lastSourceEntry, newContentItem);
                }
            }
        }

        return loaded;
    }

    private IEnumerable<ContentEntry> GetAllEntriesFromSource(IContentSource source)
    {
        return source.GetStructure().GetEntries(entry => true);
    }

    public async Task LoadAsync()
    {
        var validSources = this.CollectSourcesWithValidStructures();
        var validSourcesOrdered = this._loader.GetSourceLoadOrder(validSources).ToList();

        var allEntriesWithTheirSource = validSourcesOrdered
            .SelectMany(source => this.GetAllEntriesFromSource(source)
                                  .Select(entry => (source, entry))).ToList();

        var entriesGroupedByEntryPath = allEntriesWithTheirSource.GroupBy(pair => pair.entry.EntryPath).ToList();

        var entryPathToFirstSource = entriesGroupedByEntryPath
            .ToDictionary(grouping => grouping.Key, x => x.OrderBy(y => validSourcesOrdered.IndexOf(y.source)).First().source);

        var entryPathToLastSource = entriesGroupedByEntryPath
            .ToDictionary(grouping => grouping.Key, x => x.OrderBy(y => validSourcesOrdered.IndexOf(y.source)).Last().source);

        var entryPathToLastSourceEntry = entriesGroupedByEntryPath
            .ToDictionary(grouping => grouping.Key, x => x.OrderBy(y => validSourcesOrdered.IndexOf(y.source)).Last().entry);

        var entriesToLoad = new List<(IContentSource firstSource, IContentSource lastSource, ContentEntry entry)>();

        foreach (var grouping in entriesGroupedByEntryPath)
        {
            string entryPath = grouping.Key;

            if (_overwriter.IsEntryAffectedByOverwrite(entryPath))
            {
                entriesToLoad.Add((entryPathToFirstSource[entryPath], entryPathToLastSource[entryPath], entryPathToLastSourceEntry[entryPath]));
            }
            else
            {
                foreach (var entry in grouping)
                {
                    entriesToLoad.Add((entry.source, entry.source, entry.entry));
                }
            }
        }

        var stagesToRun = this._loader.GetLoadingStages();

        var previouslyLoadedContent = this._loadedContent.GetCopy();

        var newLoadedContent = new ContentCollection();
        foreach (var stage in stagesToRun)
        {
            newLoadedContent = await this.RunStageAsync(entriesToLoad, stage, newLoadedContent);
        }

        this._loadedContent = newLoadedContent;

        foreach (var item in this._loadedContent.GetItems())
        {
            if (previouslyLoadedContent.HasItem(item.Identifier!))
            {
                this._loadedContent.ReplaceContentItem(item.Identifier!, previouslyLoadedContent.GetContentItem(item.Identifier!)!);
                this._loadedContent.GetContentItem(item.Identifier!)!.UpdateContent(item.Content);
            }
        }
    }

    public IContent? GetContent(string identifier)
    {
        return this._loadedContent.GetContentItem(identifier)!.Content;
    }

    public T? GetContent<T>(string identifier) where T : IContent
    {
        return this._loadedContent.GetContentItem<T>(identifier);
    }

    public IEnumerable<ContentItem> GetContentItems()
    {
        return this._loadedContent.GetItems();
    }

    public async Task PollForSourceUpdatesAsync()
    {
        var itemsToReload = new List<ContentItem>();

        foreach (var item in this._loadedContent.GetItems())
        {
            var entry = this._loadedContent.GetEntryForItem(item.Identifier!);

            var recordedLastWriteTime = entry.LastWriteTime;
            using var structure = item.FinalSource.GetStructure();
            var currentLastWriteTime = structure.GetLastWriteTimeForEntry(entry.EntryPath);

            if (currentLastWriteTime > recordedLastWriteTime)
            {
                itemsToReload.Add(item);
            }
        }

        if (itemsToReload.Count == 0)
        {
            return;
        }

        var stages = this._loader.GetLoadingStages();

        var updatedContent = new ContentCollection();
        foreach (var stage in stages)
        {
            updatedContent = await RunStageAsync(
                itemsToReload.Select(i => (i.SourceFirstLoadedIn, i.FinalSource, this._loadedContent.GetEntryForItem(i.Identifier))),
                stage,
                updatedContent
            );
        }

        foreach (var (updatedEntry, updatedItems) in updatedContent.GetEntriesAndItems())
        {
            foreach (var updatedItem in updatedItems)
            {
                var previousItem = this._loadedContent.GetContentItem(updatedItem.Identifier)!;
                previousItem.UpdateContent(updatedItem.Content);

                var previousEntry = this._loadedContent.GetEntryForItem(updatedItem.Identifier);
                previousEntry.LastWriteTime = updatedEntry.LastWriteTime;
            }
        }
    }

    public void UnloadAllContent()
    {
        foreach (var item in this._loadedContent.GetItems())
        {
            item.Content.Unload();
        }
    }
}