using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Symphony;

public interface IContentLoadingStage
{
    string StageName { get; }
    IEnumerable<ContentEntry> GetAffectedEntries(IEnumerable<ContentEntry> allEntries);
    bool TryLoadEntry(IContentSource source, IContentStructure structure, ContentEntry entry, [NotNullWhen(false)] out string? error, [NotNullWhen(true)] out ContentItem? item);
}

public interface IContentLoader<TMeta> where TMeta : ContentMetadata
{
    IEnumerable<IContentLoadingStage> GetSynchronousStages();
    IEnumerable<IContentLoadingStage> GetLoadingStages();
}