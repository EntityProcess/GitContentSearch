namespace GitContentSearch
{
    public interface IGitLocator
    {
        (string? CommitHash, string? FilePath) LocateFile(string fileName);
    }
} 