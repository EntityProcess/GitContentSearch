using System.Diagnostics;

namespace GitContentSearch
{
    public class ProcessWrapper : IProcessWrapper
    {
        public ProcessResult Start(ProcessStartInfo startInfo)
        {
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new Exception("Failed to start process.");
                }

                string standardOutput = process.StandardOutput.ReadToEnd().Trim();
                string standardError = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();  // Ensure the process has completed

                int exitCode = process.ExitCode;

                return new ProcessResult(standardOutput, standardError, exitCode);
            }
        }
    }
}
