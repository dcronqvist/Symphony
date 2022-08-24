using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Symphony.Common;

public class ZipFileContentStructure : IContentStructure
{
    private string _pathToZip;
    private ZipArchive _archive;
    private Stream _streamForArchive;
    private bool disposedValue;

    public ZipFileContentStructure(string pathToZip)
    {
        _pathToZip = pathToZip;
        _streamForArchive = File.OpenRead(_pathToZip);
        _archive = new ZipArchive(_streamForArchive);
    }

    public IEnumerable<string> GetAllFilesInContent()
    {
        return _archive.Entries.Select(e => e.FullName);
    }

    public Stream GetFileStream(string fileInContent)
    {
        return _archive.GetEntry(fileInContent)!.Open();
    }

    public bool HasFile(string fileInContent)
    {
        return this._archive.Entries.Any(e => e.FullName == fileInContent);
    }

    public bool HasFolder(string folderInContent)
    {
        return this._archive.Entries.Any(e => e.FullName.StartsWith(folderInContent));
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
                this._streamForArchive.Dispose();
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
}