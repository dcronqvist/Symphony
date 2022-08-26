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

public class LoadingStageEventArgs<TMeta> : EventArgs where TMeta : ContentMetadata
{
    public IContentLoadingStage<TMeta> Stage { get; }
    public ContentCollection CurrentlyLoaded { get; }

    public LoadingStageEventArgs(IContentLoadingStage<TMeta> stage, ContentCollection currentlyLoaded)
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
    private Dictionary<string, ContentItem> _items = new Dictionary<string, ContentItem>();

    public void AddItem(ContentItem item)
    {
        _items.Add(item.Identifier, item);
    }

    public void RemoveItem(string identifier)
    {
        _items.Remove(identifier);
    }

    public ContentItem? GetContentItem(string identifier)
    {
        if (this._items.TryGetValue(identifier, out var item))
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
        if (this._items.TryGetValue(identifier, out var item))
        {
            return (T)item;
        }
        else
        {
            return null;
        }
    }

    public ContentCollection GetCopy()
    {
        var copy = new ContentCollection();
        foreach (var item in _items)
        {
            copy.AddItem(item.Value);
        }
        return copy;
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
    public event EventHandler<LoadingStageEventArgs<TMeta>>? StartedLoadingStage;
    public event EventHandler<LoadingStageEventArgs<TMeta>>? FinishedLoadingStage;
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

        var currentlyLoadedContent = new ContentCollection();

        var progress = new Progress<string>((path) =>
        {
            this.ContentItemStartedLoading?.Invoke(this, new ContentItemStartedLoadingEventArgs(path));
        });

        foreach (var stage in stages)
        {
            this.StartedLoadingStage?.Invoke(this, new LoadingStageEventArgs<TMeta>(stage, currentlyLoadedContent));

            foreach (var source in sources)
            {
                var meta = this._validMods[source];

                try
                {
                    using (var structure = source.GetStructure())
                    {
                        currentlyLoadedContent = stage.LoadContent(meta, source, structure, currentlyLoadedContent, progress);
                    }
                }
                catch (Exception ex)
                {
                    this.ContentFailedToLoadError?.Invoke(this, new ContentFailedToLoadErrorEventArgs(ex.Message, source));
                }
            }

            this.FinishedLoadingStage?.Invoke(this, new LoadingStageEventArgs<TMeta>(stage, currentlyLoadedContent));
        }

        this._loadedContent = currentlyLoadedContent;

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
}