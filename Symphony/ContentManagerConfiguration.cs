namespace Symphony;

public class ContentManagerConfiguration<TMeta> where TMeta : ContentMetadata
{
    public IContentStructureValidator<TMeta> StructureValidator { get; private set; }
    public IContentCollectionProvider CollectionProvider { get; private set; }
    public IContentLoader<TMeta> Loader { get; private set; }
    public bool HotReload { get; private set; }
    public bool SameEntryPathOverwrites { get; private set; } = true;

    public ContentManagerConfiguration(IContentStructureValidator<TMeta> validator, IContentCollectionProvider collectionProvider, IContentLoader<TMeta> loader, bool hotReload = false, bool sameEntryPathOverwrites = true)
    {
        StructureValidator = validator;
        CollectionProvider = collectionProvider;
        Loader = loader;
        HotReload = hotReload;
        SameEntryPathOverwrites = sameEntryPathOverwrites;
    }
}