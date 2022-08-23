namespace Symphony;

public class ContentManagerConfiguration<TMeta> where TMeta : ContentMetadata
{
    internal IContentStructureValidator<TMeta> ModStructureValidator { get; private set; }
    internal IContentCollectionProvider ModCollectionProvider { get; private set; }
    internal IContentLoader<TMeta> ModLoader { get; private set; }

    public ContentManagerConfiguration(IContentStructureValidator<TMeta> validator, IContentCollectionProvider collectionProvider, IContentLoader<TMeta> loader)
    {
        ModStructureValidator = validator;
        ModCollectionProvider = collectionProvider;
        ModLoader = loader;
    }
}