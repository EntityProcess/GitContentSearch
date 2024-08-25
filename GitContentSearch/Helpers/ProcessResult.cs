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