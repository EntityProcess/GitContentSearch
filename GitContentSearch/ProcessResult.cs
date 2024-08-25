using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitContentSearch
{
    public class ProcessResult
    {
        public string StandardOutput { get; }
        public string StandardError { get; }
        public int ExitCode { get; }

        public ProcessResult(string standardOutput, string standardError, int exitCode)
        {
            StandardOutput = standardOutput;
            StandardError = standardError;
            ExitCode = exitCode;
        }
    }
}
