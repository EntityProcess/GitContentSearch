using System;

namespace GitContentSearch.Interfaces
{
    public interface IGitLocator
    {
        (string? CommitHash, string? FilePath) LocateFile(string fileName, IProgress<double>? progress = null);
    }
} 