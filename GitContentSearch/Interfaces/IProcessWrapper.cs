using System.Diagnostics;
using System.Threading;

namespace GitContentSearch
{
    public interface IProcessWrapper
    {
        ProcessResult Start(ProcessStartInfo startInfo, Stream? outputStream = null);
		ProcessResult Start(string arguments, string? workingDirectory, Stream? outputStream);
        ProcessResult Start(string arguments, string? workingDirectory, Stream? outputStream, CancellationToken cancellationToken);
        void StartAndProcessOutput(string arguments, string? workingDirectory, Action<string> lineProcessor);
        void StartAndProcessOutput(string arguments, string? workingDirectory, Action<string> lineProcessor, CancellationToken cancellationToken);
	}
}
