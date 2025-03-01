using System;

namespace GitContentSearch.Interfaces
{
    public interface ISearchLogger : IDisposable
    {
        void LogHeader(string operation, string workingDirectory, string targetFile, string? tempDirectory = null);
        void LogProgress(string progressMessage);
        void LogFooter();
        void LogError(string message, Exception? ex = null);
        void WriteLine(string message);
        void Flush();
    }
} 