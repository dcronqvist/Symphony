using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace Symphony.Common;

public class EmbeddedSource : IContentSource
{
    private Assembly _assembly;

    public EmbeddedSource(Assembly assembly)
    {
        _assembly = assembly;
    }

    public IContentStructure GetStructure()
    {
        return new EmbeddedStructure(this._assembly);
    }

    public override string ToString()
    {
        return $"EmbeddedSource: {_assembly.FullName}";
    }
}