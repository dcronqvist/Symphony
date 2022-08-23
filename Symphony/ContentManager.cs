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

public class ContentManager<TMeta> where TMeta : ContentMetadata
{
    // Manager specific stuff
    private readonly ContentManagerConfiguration<TMeta> _configuration;
    private Dictionary<IContentSource, TMeta> _validMods;
    private Dictionary<string, ContentItem> _loadedContentItems;

    // Events
    public event EventHandler<ContentStructureErrorEventArgs>? InvalidContentStructureError;

    public ContentManager(ContentManagerConfiguration<TMeta> configuration)
    {
        this._configuration = configuration;
        this._validMods = new Dictionary<IContentSource, TMeta>();
        this._loadedContentItems = new Dictionary<string, ContentItem>();
    }

    private IEnumerable<IContentSource> CollectValidMods()
    {
        this._validMods.Clear();

        var modSources = this._configuration.ModCollectionProvider.GetModSources();

        foreach (var source in modSources)
        {
            using (var structure = source.GetStructure())
            {
                if (this._configuration.ModStructureValidator.TryValidateMod(structure, out var metadata, out string? error))
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
        var sources = this.CollectValidMods();

        foreach (var source in sources)
        {
            var meta = this._validMods[source];
            var loadedItems = this._configuration.ModLoader.LoadContent(meta, source);

            foreach (var loadedItem in loadedItems)
            {
                if (this._loadedContentItems.ContainsKey(loadedItem.Identifier))
                {
                    this._loadedContentItems[loadedItem.Identifier].UpdateContent(source, loadedItem.Content);
                }
                else
                {
                    this._loadedContentItems.Add(loadedItem.Identifier, loadedItem);
                }
            }
        }
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