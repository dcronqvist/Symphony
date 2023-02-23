using System;

namespace Symphony;

public sealed class ContentEntry
{
    private string _entryPath;
    public string EntryPath
    {
        get => _entryPath;
        private set => _entryPath = value.Replace('\\', '/');
    }
    public DateTime LastWriteTime { get; private set; }

    public ContentEntry(string entryPath)
    {
        _entryPath = "";
        EntryPath = entryPath;
    }

    internal void SetLastWriteTime(DateTime lastWriteTime)
    {
        LastWriteTime = lastWriteTime;
    }

    public override string ToString()
    {
        return $"ContentEntry: {EntryPath} @ {LastWriteTime}";
    }
}