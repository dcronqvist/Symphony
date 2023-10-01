using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Symphony.Common;

public class EmbeddedStructure : IContentStructure
{
    private Assembly _assembly;
    private bool disposedValue;

    public EmbeddedStructure(Assembly assembly)
    {
        _assembly = assembly;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ZipFileContentStructure()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public bool HasEntry(string entryPath)
    {
        var entries = this._assembly.GetManifestResourceNames();
        entries = entries.Select(x => x.Replace('.', '/')).ToArray();
        return entries.Contains(entryPath);
    }

    public ContentEntry GetEntry(string entryPath)
    {
        return new ContentEntry(entryPath);
    }

    public IEnumerable<ContentEntry> GetEntries(Predicate<ContentEntry>? filter = null)
    {
        var entries = this._assembly.GetManifestResourceNames();
        entries = entries.Select(x => x.Replace('.', '/')).ToArray();
        return entries.Select(x => new ContentEntry(x)).Where(x => filter?.Invoke(x) ?? true);
    }

    public DateTime GetLastWriteTimeForEntry(string entryPath)
    {
        var path = entryPath.Replace('/', '.');
        return File.GetLastWriteTime(this._assembly.Location);
    }

    public Stream GetEntryStream(string entryPath)
    {
        return this._assembly.GetManifestResourceStream(entryPath.Replace('/', '.'))!;
    }
}