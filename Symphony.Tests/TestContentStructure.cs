using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Symphony.Tests;

internal class TestContentEntry
{
    public string Name { get; set; }
    public byte[] Data { get; set; }

    internal TestContentEntry(string name, byte[] data)
    {
        Name = name;
        Data = data;
    }
}

internal class TestContentStructure : IContentStructure
{
    private TestContentEntry[] _entries;
    private bool disposedValue;

    public TestContentStructure(params TestContentEntry[] entries)
    {
        _entries = entries;
    }

    public bool HasFile(string fileInContent)
    {
        return this._entries.Any(e => e.Name == fileInContent);
    }

    public bool HasFolder(string folderInContent)
    {
        return this._entries.Any(e => e.Name.StartsWith(folderInContent));
    }

    public bool TryGetFileStream(string fileInContent, [NotNullWhen(true)] out Stream? stream)
    {
        if (this.HasFile(fileInContent))
        {
            stream = GetFileStream(fileInContent);
            return true;
        }
        else
        {
            stream = null;
            return false;
        }
    }

    public Stream GetFileStream(string fileInContent)
    {
        var entry = this._entries.First(e => e.Name == fileInContent);
        return new MemoryStream(entry.Data);
    }

    public IEnumerable<string> GetAllFilesInContent()
    {
        return this._entries.Select(e => e.Name);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                // Nothing to do.
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~TestContentStructure()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        System.GC.SuppressFinalize(this);
    }
}