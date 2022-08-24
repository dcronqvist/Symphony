using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

namespace Symphony;

public interface IContentStructure : IDisposable
{
    bool HasFile(string fileInContent);
    bool HasFolder(string folderInContent);
    bool TryGetFileStream(string fileInContent, [NotNullWhen(true)] out Stream? stream);
    Stream GetFileStream(string fileInContent);
    IEnumerable<string> GetAllFilesInContent();
}