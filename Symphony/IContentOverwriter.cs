namespace Symphony;

public interface IContentOverwriter
{
    bool IsEntryAffectedByOverwrite(string entryPath);
}