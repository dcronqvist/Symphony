namespace Symphony;

public interface IContentLoader<TMeta> where TMeta : ContentMetadata
{
    IEnumerable<ContentItem> LoadContent(TMeta metadata, IContentSource source);
}