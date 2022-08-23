using System.Diagnostics.CodeAnalysis;

namespace Symphony;

public interface IContentStructureValidator<TMeta> where TMeta : ContentMetadata
{
    bool TryValidateMod(IContentStructure structure, [NotNullWhen(true)] out TMeta? metadata, [NotNullWhen(false)] out string? error);
}