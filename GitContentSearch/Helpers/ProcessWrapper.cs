﻿using GitContentSearch;
using System.Diagnostics;

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
