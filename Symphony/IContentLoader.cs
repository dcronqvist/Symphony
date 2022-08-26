using System.Collections.Generic;
using System;

namespace Symphony;

public interface IContentLoadingStage<TMeta> where TMeta : ContentMetadata
{
    string StageName { get; }
    ContentCollection LoadContent(TMeta metadata, IContentSource source, IContentStructure structure, ContentCollection currentLoadedContent, IProgress<string> progress);
}

public interface IContentLoader<TMeta> where TMeta : ContentMetadata
{
    // /// <summary>
    // /// Load the content from the mod source, if any error occurs, throw an exception and it will be caught by the ContentManager.
    // /// </summary>
    // IEnumerable<ContentItem> LoadContent(TMeta metadata, IContentSource source);
    IEnumerable<IContentLoadingStage<TMeta>> GetLoadingStages();
}