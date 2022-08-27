using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Symphony;

public interface IContentLoadingStage
{
    string StageName { get; }
    IEnumerable<ContentEntry> GetAffectedEntries(IEnumerable<ContentEntry> allEntries);
    bool TryLoadEntry(IContentStructure structure, ContentEntry entry, [NotNullWhen(false)] out string? error, [NotNullWhen(true)] out ContentItem? item);
}

public interface IContentLoader<TMeta> where TMeta : ContentMetadata
{
    // /// <summary>
    // /// Load the content from the mod source, if any error occurs, throw an exception and it will be caught by the ContentManager.
    // /// </summary>
    // IEnumerable<ContentItem> LoadContent(TMeta metadata, IContentSource source);
    IEnumerable<IContentLoadingStage> GetLoadingStages();
}