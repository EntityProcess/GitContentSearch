using System.Threading;

namespace GitContentSearch.Interfaces
{
	public interface IGitFileLocator
    {
        (string? CommitHash, string? FilePath) LocateFile(string fileName, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    }
} 