using System;
using System.Collections.Generic;
using System.Linq;

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

    public LoadingStageEventArgs(IContentLoadingStage<TMeta> stage)
    {
        Stage = stage;
    }
}

public class ContentManager<TMeta> where TMeta : ContentMetadata
{
    // Manager specific stuff
    private readonly ContentManagerConfiguration<TMeta> _configuration;
    private Dictionary<IContentSource, TMeta> _validMods;
    private Dictionary<string, ContentItem> _loadedContentItems;

    // Events
    public event EventHandler? StartedLoading;
    public event EventHandler<LoadingStageEventArgs<TMeta>>? StartedLoadingStage;
    public event EventHandler<LoadingStageEventArgs<TMeta>>? FinishedLoadingStage;
    public event EventHandler<ContentStructureErrorEventArgs>? InvalidContentStructureError;
    public event EventHandler<ContentFailedToLoadErrorEventArgs>? ContentFailedToLoadError;
    public event EventHandler? FinishedLoading;

    public ContentManager(ContentManagerConfiguration<TMeta> configuration)
    {
        this._configuration = configuration;
        this._validMods = new Dictionary<IContentSource, TMeta>();
        this._loadedContentItems = new Dictionary<string, ContentItem>();
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

    public void Load()
    {
        this.StartedLoading?.Invoke(this, EventArgs.Empty);

        var sources = this.CollectValidMods();
        var stages = this._configuration.Loader.GetLoadingStages();

        var currentlyLoadedContent = new List<ContentItem>();

        foreach (var stage in stages)
        {
            this.StartedLoadingStage?.Invoke(this, new LoadingStageEventArgs<TMeta>(stage));

            foreach (var source in sources)
            {
                var meta = this._validMods[source];

                try
                {
                    using (var structure = source.GetStructure())
                    {
                        currentlyLoadedContent = stage.LoadContent(meta, source, structure, currentlyLoadedContent).ToList();
                    }
                }
                catch (Exception ex)
                {
                    this.ContentFailedToLoadError?.Invoke(this, new ContentFailedToLoadErrorEventArgs(ex.Message, source));
                }
            }

            this.FinishedLoadingStage?.Invoke(this, new LoadingStageEventArgs<TMeta>(stage));
        }

        var removedContent = this._loadedContentItems.ToDictionary(x => x.Key, x => x.Value);
        foreach (var loaded in currentlyLoadedContent)
        {
            removedContent.Remove(loaded.Identifier);

            if (this._loadedContentItems.ContainsKey(loaded.Identifier))
            {
                this._loadedContentItems[loaded.Identifier].UpdateContent(loaded.Source, loaded.Content);
            }
            else
            {
                this._loadedContentItems.Add(loaded.Identifier, loaded);
            }
        }

        foreach (var removed in removedContent)
        {
            this._loadedContentItems.Remove(removed.Key);
        }

        this.FinishedLoading?.Invoke(this, EventArgs.Empty);
    }

    public ContentItem? GetContentItem(string identifier)
    {
        if (this._loadedContentItems.TryGetValue(identifier, out var item))
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
        if (this._loadedContentItems.TryGetValue(identifier, out var item))
        {
            return (T)item;
        }
        else
        {
            return null;
        }
    }
}