using System.Diagnostics;

namespace GitContentSearch
{
    public interface IProcessWrapper
    {
        ProcessResult Start(ProcessStartInfo startInfo, Stream? outputStream = null);
		ProcessResult Start(string arguments, string? workingDirectory, Stream? outputStream);
	}
}
