using System.Collections.Generic;

namespace Symphony;

public interface IContentLoadingStage<TMeta> where TMeta : ContentMetadata
{
    string StageName { get; }
    IEnumerable<ContentItem> LoadContent(TMeta metadata, IContentSource source, IContentStructure structure, IEnumerable<ContentItem> currentLoadedContent);
}

public interface IContentLoader<TMeta> where TMeta : ContentMetadata
{
    // /// <summary>
    // /// Load the content from the mod source, if any error occurs, throw an exception and it will be caught by the ContentManager.
    // /// </summary>
    // IEnumerable<ContentItem> LoadContent(TMeta metadata, IContentSource source);
    IEnumerable<IContentLoadingStage<TMeta>> GetLoadingStages();
}