using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Symphony;

public struct LoadEntryResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public ContentItem? Item { get; set; }
    public string? Identifier { get; set; }

    public static LoadEntryResult CreateSuccess(string identifier, ContentItem item)
    {
        return new LoadEntryResult
        {
            Identifier = identifier,
            Success = true,
            Item = item
        };
    }

    public static LoadEntryResult CreateFailure(string error)
    {
        return new LoadEntryResult
        {
            Success = false,
            Error = error
        };
    }

    public static async Task<LoadEntryResult> CreateSuccessAsync(string identifier, ContentItem item)
    {
        return await Task.FromResult(CreateSuccess(identifier, item));
    }

    public static async Task<LoadEntryResult> CreateFailureAsync(string error)
    {
        return await Task.FromResult(CreateFailure(error));
    }
}

public interface IContentLoadingStage
{
    string StageName { get; }
    IEnumerable<ContentEntry> GetAffectedEntries(IEnumerable<ContentEntry> allEntries);
    void OnStageStarted();
    IAsyncEnumerable<LoadEntryResult> TryLoadEntry(IContentSource source, IContentStructure structure, ContentEntry entry);
    void OnStageCompleted();
}

public interface IContentLoader<TMeta> where TMeta : ContentMetadata
{
    IEnumerable<IContentSource> GetSourceLoadOrder(IEnumerable<IContentSource> sources);
    IEnumerable<IContentLoadingStage> GetLoadingStages();
}