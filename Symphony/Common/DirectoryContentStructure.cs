using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Symphony.Common;

public class DirectoryContentStructure : IContentStructure
{
    private string _contentRoot;
    private bool disposedValue;

    public DirectoryContentStructure(string contentRoot)
    {
        _contentRoot = contentRoot;
    }

    public IEnumerable<string> GetAllFilesInContent()
    {
        return Directory.EnumerateFiles(_contentRoot).Select(p => Path.GetRelativePath(_contentRoot, p));
    }

    public Stream GetFileStream(string fileInContent)
    {
        return File.OpenRead(Path.Combine(_contentRoot, fileInContent));
    }

    public bool HasFile(string fileInContent)
    {
        return File.Exists(Path.Combine(_contentRoot, fileInContent));
    }

    public bool HasFolder(string folderInContent)
    {
        return Directory.Exists(Path.Combine(_contentRoot, folderInContent));
    }

    public bool TryGetFileStream(string fileInContent, [NotNullWhen(true)] out Stream? stream)
    {
        if (HasFile(fileInContent))
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
    // ~DirectoryContentStructure()
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

    public IEnumerable<string> GetAllFilesInFolder(string folderPath)
    {
        return Directory.EnumerateFiles(Path.Combine(_contentRoot, folderPath), "*.*", SearchOption.AllDirectories).Select(p => Path.GetRelativePath(_contentRoot, p));
    }
}