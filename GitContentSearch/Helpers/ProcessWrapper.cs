using GitContentSearch;
using System.Diagnostics;
using System.Threading.Tasks;

public class ProcessWrapper : IProcessWrapper
{
    public ProcessResult Start(ProcessStartInfo startInfo)
    {
        return StartInternal(startInfo, null);
    }

    public ProcessResult Start(ProcessStartInfo startInfo, Stream? outputStream)
    {
        return StartInternal(startInfo, outputStream);
    }

	public ProcessResult Start(string arguments, string? workingDirectory, Stream? outputStream)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "git",
			Arguments = arguments,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = workingDirectory
		};

		return StartInternal(startInfo, outputStream);
	}

    public void StartAndProcessOutput(string arguments, string? workingDirectory, Action<string> lineProcessor)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new Exception("Failed to start process.");
        }

        try
        {
            // Process output line by line
            string? line;
            while (!process.HasExited && (line = process.StandardOutput.ReadLine()) != null)
            {
                lineProcessor(line);
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // Best effort to kill the process
                }
            }
        }
    }

	private ProcessResult StartInternal(ProcessStartInfo startInfo, Stream? outputStream)
    {
        using (var process = Process.Start(startInfo))
        {
            if (process == null)
            {
                throw new Exception("Failed to start process.");
            }

            string standardOutput = string.Empty;
            if (outputStream == null)
            {
                standardOutput = process.StandardOutput.ReadToEnd().Trim();
            }
            else
            {
                process.StandardOutput.BaseStream.CopyTo(outputStream);
                outputStream.Position = 0; // Reset stream position for reading
            }

            string standardError = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();

            return new ProcessResult(standardOutput, standardError, process.ExitCode);
        }
    }
}
