namespace Symphony;

public class ContentManagerConfiguration<TMeta> where TMeta : ContentMetadata
{
    public IContentStructureValidator<TMeta> StructureValidator { get; private set; }
    public IContentCollectionProvider CollectionProvider { get; private set; }
    public IContentLoader<TMeta> Loader { get; private set; }
    public bool HotReload { get; private set; }

    public ContentManagerConfiguration(IContentStructureValidator<TMeta> validator, IContentCollectionProvider collectionProvider, IContentLoader<TMeta> loader, bool hotReload = false)
    {
        StructureValidator = validator;
        CollectionProvider = collectionProvider;
        Loader = loader;
        HotReload = hotReload;
    }
}