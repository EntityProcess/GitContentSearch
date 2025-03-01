using GitContentSearch;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

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

    public ProcessResult Start(string arguments, string? workingDirectory, Stream? outputStream, CancellationToken cancellationToken)
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

        return StartInternal(startInfo, outputStream, cancellationToken);
    }

    public void StartAndProcessOutput(string arguments, string? workingDirectory, Action<string> lineProcessor)
    {
        StartAndProcessOutput(arguments, workingDirectory, lineProcessor, CancellationToken.None);
    }

    public void StartAndProcessOutput(string arguments, string? workingDirectory, Action<string> lineProcessor, CancellationToken cancellationToken)
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
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                
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
        
        // If cancelled, throw OperationCanceledException to signal cancellation
        cancellationToken.ThrowIfCancellationRequested();
    }

	private ProcessResult StartInternal(ProcessStartInfo startInfo, Stream? outputStream)
    {
        return StartInternal(startInfo, outputStream, CancellationToken.None);
    }

    private ProcessResult StartInternal(ProcessStartInfo startInfo, Stream? outputStream, CancellationToken cancellationToken)
    {
        using (var process = Process.Start(startInfo))
        {
            if (process == null)
            {
                throw new Exception("Failed to start process.");
            }

            string standardOutput = string.Empty;
            string standardError = string.Empty;
            
            try
            {
                if (outputStream == null)
                {
                    // Read output line by line instead of ReadToEnd() to allow for cancellation
                    using (var outputBuilder = new StringWriter())
                    {
                        string? line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            // Check for cancellation between reading lines
                            cancellationToken.ThrowIfCancellationRequested();
                            
                            outputBuilder.WriteLine(line);
                        }
                        standardOutput = outputBuilder.ToString().Trim();
                    }
                }
                else
                {
                    // Use a buffer to read chunks and check for cancellation periodically
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = process.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        // Check for cancellation between reading chunks
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        outputStream.Write(buffer, 0, bytesRead);
                    }
                    outputStream.Position = 0; // Reset stream position for reading
                }

                // Read error output line by line as well
                using (var errorBuilder = new StringWriter())
                {
                    string? line;
                    while ((line = process.StandardError.ReadLine()) != null)
                    {
                        // Check for cancellation between reading lines
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        errorBuilder.WriteLine(line);
                    }
                    standardError = errorBuilder.ToString().Trim();
                }

                process.WaitForExit();
            }
            catch (OperationCanceledException)
            {
                // If cancelled, kill the process and rethrow
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
                throw;
            }

            // If cancelled after process completed, still throw
            cancellationToken.ThrowIfCancellationRequested();

            return new ProcessResult(standardOutput, standardError, process.ExitCode);
        }
    }
}
