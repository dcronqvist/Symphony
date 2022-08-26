namespace Symphony;

public class ContentManagerConfiguration<TMeta> where TMeta : ContentMetadata
{
    internal IContentStructureValidator<TMeta> StructureValidator { get; private set; }
    internal IContentCollectionProvider CollectionProvider { get; private set; }
    internal IContentLoader<TMeta> Loader { get; private set; }

    public ContentManagerConfiguration(IContentStructureValidator<TMeta> validator, IContentCollectionProvider collectionProvider, IContentLoader<TMeta> loader)
    {
        StructureValidator = validator;
        CollectionProvider = collectionProvider;
        Loader = loader;
    }
}