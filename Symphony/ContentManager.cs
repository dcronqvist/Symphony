using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Symphony;

public class ContentStructureErrorEventArgs : EventArgs
{
    public string Error { get; }
    public IContentSource Source { get; }

    public ContentStructureErrorEventArgs(string error, IContentSource source)
    {
        Error = error;
        Source = source;
    }
}

public class ContentFailedToLoadErrorEventArgs : EventArgs
{
    public string Error { get; }
    public IContentSource Source { get; }

    public ContentFailedToLoadErrorEventArgs(string error, IContentSource source)
    {
        Error = error;
        Source = source;
    }
}

public class LoadingStageEventArgs : EventArgs
{
    public IContentLoadingStage Stage { get; }
    public ContentCollection CurrentlyLoaded { get; }

    public LoadingStageEventArgs(IContentLoadingStage stage, ContentCollection currentlyLoaded)
    {
        Stage = stage;
        CurrentlyLoaded = currentlyLoaded;
    }
}

public class ContentItemStartedLoadingEventArgs : EventArgs
{
    public string ItemPath { get; }

    public ContentItemStartedLoadingEventArgs(string itemPath)
    {
        ItemPath = itemPath;
    }
}

public class ContentCollection
{
    // From content item identifier to entry.
    private Dictionary<string, ContentEntry> _entries = new Dictionary<string, ContentEntry>();

    // From entry to item
    private Dictionary<ContentEntry, ContentItem> _items = new Dictionary<ContentEntry, ContentItem>();

    public bool HasItem(string identifier)
    {
        var entry = this._entries.GetValueOrDefault(identifier);
        return entry != null;
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
        this._entries.Add(item.Identifier, entry);
        this._items.Add(entry, item);
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

        if (this._items.TryGetValue(entry, out var item))
        {
            return item;
        }
        else
        {
            return null;
        }
    }

    public T? GetContentItem<T>(string identifier) where T : ContentItem
    {
        var entry = this._entries.GetValueOrDefault(identifier);
        if (entry == null)
            return null;

        if (this._items.TryGetValue(entry, out var item))
        {
            return (T)item;
        }
        else
        {
            return null;
        }
    }

    public void ReplaceContentItem(string identifier, ContentItem item)
    {
        var entry = this._entries.GetValueOrDefault(identifier);
        if (entry == null)
            return;
        this._items[entry] = item;
    }

    public ContentCollection GetCopy()
    {
        var copy = new ContentCollection();
        foreach (var item in _items)
        {
            copy.AddItem(item.Key, item.Value);
        }
        return copy;
    }

    public IEnumerable<ContentItem> GetItems()
    {
        return _items.Values;
    }
}

public class ContentManager<TMeta> where TMeta : ContentMetadata
{
    // Manager specific stuff
    private readonly ContentManagerConfiguration<TMeta> _configuration;
    private Dictionary<IContentSource, TMeta> _validMods;
    private ContentCollection _loadedContent;

    // Events
    public event EventHandler? StartedLoading;
    public event EventHandler<LoadingStageEventArgs>? StartedLoadingStage;
    public event EventHandler<LoadingStageEventArgs>? FinishedLoadingStage;
    public event EventHandler<ContentStructureErrorEventArgs>? InvalidContentStructureError;
    public event EventHandler<ContentFailedToLoadErrorEventArgs>? ContentFailedToLoadError;
    public event EventHandler<ContentItemStartedLoadingEventArgs>? ContentItemStartedLoading;
    public event EventHandler? FinishedLoading;

    public ContentManager(ContentManagerConfiguration<TMeta> configuration)
    {
        this._configuration = configuration;
        this._validMods = new Dictionary<IContentSource, TMeta>();
        this._loadedContent = new ContentCollection();
    }

    private IEnumerable<IContentSource> CollectValidMods()
    {
        this._validMods.Clear();

        var modSources = this._configuration.CollectionProvider.GetModSources();

        foreach (var source in modSources)
        {
            using (var structure = source.GetStructure())
            {
                if (this._configuration.StructureValidator.TryValidateMod(structure, out var metadata, out string? error))
                {
                    // Mod is valid and can be loaded
                    this._validMods.Add(source, metadata);
                    yield return source;
                }
                else
                {
                    // Mod is invalid and cannot be loaded
                    this.InvalidContentStructureError?.Invoke(this, new ContentStructureErrorEventArgs(error, source));
                }
            }
        }
    }

    public async Task LoadAsync()
    {
        await Task.Run(() => this.Load());
    }

    public void Load()
    {
        this.StartedLoading?.Invoke(this, EventArgs.Empty);

        var sources = this.CollectValidMods();
        var stages = this._configuration.Loader.GetLoadingStages();

        var previouslyLoaded = this._loadedContent.GetCopy();

        var currentlyLoadedContent = new ContentCollection();

        var progress = new Progress<string>((path) =>
        {
            this.ContentItemStartedLoading?.Invoke(this, new ContentItemStartedLoadingEventArgs(path));
        });

        foreach (var stage in stages)
        {
            this.StartedLoadingStage?.Invoke(this, new LoadingStageEventArgs(stage, currentlyLoadedContent));

            foreach (var source in sources)
            {
                var meta = this._validMods[source];

                try
                {
                    using (var structure = source.GetStructure())
                    {
                        var affectedEntries = stage.GetAffectedEntries(structure.GetEntries());

                        foreach (var entry in affectedEntries)
                        {
                            if (stage.TryLoadEntry(structure, entry, out string? error, out ContentItem? item))
                            {
                                currentlyLoadedContent.AddItem(entry, item);
                                entry.SetLastWriteTime(structure.GetLastWriteTimeForEntry(entry.EntryPath));
                            }
                            else
                            {
                                this.ContentFailedToLoadError?.Invoke(this, new ContentFailedToLoadErrorEventArgs(error, source));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.ContentFailedToLoadError?.Invoke(this, new ContentFailedToLoadErrorEventArgs(ex.Message, source));
                }
            }

            this._loadedContent = currentlyLoadedContent;

            this.FinishedLoadingStage?.Invoke(this, new LoadingStageEventArgs(stage, currentlyLoadedContent));
        }

        foreach (var item in currentlyLoadedContent.GetItems())
        {
            if (previouslyLoaded.HasItem(item.Identifier))
            {
                this._loadedContent.ReplaceContentItem(item.Identifier, previouslyLoaded.GetContentItem(item.Identifier)!);
                this._loadedContent.GetContentItem(item.Identifier)!.UpdateContent(item.Source, item.Content);
            }
        }

        this.FinishedLoading?.Invoke(this, EventArgs.Empty);
    }

    public ContentItem? GetContentItem(string identifier)
    {
        return this._loadedContent.GetContentItem(identifier);
    }

    public T? GetContentItem<T>(string identifier) where T : ContentItem
    {
        return this._loadedContent.GetContentItem<T>(identifier);
    }

    public IEnumerable<ContentItem> GetContentItems()
    {
        return this._loadedContent.GetItems();
    }

    public void HotReloadNewContent()
    {
        var content = this._loadedContent;

        var items = content.GetItems().ToList();

        var toReload = new List<ContentItem>();

        foreach (var item in items)
        {
            var identifier = item.Identifier;
            var entry = content.GetEntryForItem(identifier);

            var source = item.Source;

            using (var structure = source.GetStructure())
            {
                var lastWriteTime = structure.GetLastWriteTimeForEntry(entry.EntryPath);

                if (lastWriteTime > entry.LastWriteTime)
                {
                    // Needs reload
                    toReload.Add(item);
                }
            }
        }

        var stages = this._configuration.Loader.GetLoadingStages();
        var currentlyLoadedContent = new ContentCollection();

        foreach (var stage in stages)
        {
            foreach (var item in toReload)
            {
                var source = item.Source;
                var entry = content.GetEntryForItem(item.Identifier);

                using (var structure = source.GetStructure())
                {
                    var affectedEntries = stage.GetAffectedEntries(new List<ContentEntry> { entry });

                    foreach (var e in affectedEntries)
                    {
                        if (stage.TryLoadEntry(structure, entry, out string? error, out ContentItem? reloadedItem))
                        {
                            currentlyLoadedContent.AddItem(entry, reloadedItem);
                            entry.SetLastWriteTime(structure.GetLastWriteTimeForEntry(entry.EntryPath));
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
        }

        foreach (var item in currentlyLoadedContent.GetItems())
        {
            if (content.HasItem(item.Identifier))
            {
                this._loadedContent.GetContentItem(item.Identifier)!.UpdateContent(item.Source, item.Content);
                content.GetEntryForItem(item.Identifier).SetLastWriteTime(currentlyLoadedContent.GetEntryForItem(item.Identifier).LastWriteTime);
            }
        }
    }
}