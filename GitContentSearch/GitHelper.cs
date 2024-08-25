using System.Diagnostics;

namespace GitContentSearch
{
    public class GitHelper : IGitHelper
    {
        private readonly IProcessWrapper _processWrapper;
        private readonly string? _workingDirectory;

        public GitHelper(IProcessWrapper processWrapper)
        {
            _processWrapper = processWrapper;
        }

        public GitHelper(IProcessWrapper processWrapper, string? workingDirectory)
        {
            _processWrapper = processWrapper;
            _workingDirectory = workingDirectory;
        }

        public string GetCommitTime(string commitHash)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"show -s --format=%ci {commitHash}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _workingDirectory // Set the working directory
            };

            var result = _processWrapper.Start(startInfo);

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error getting commit time: {result.StandardError}");
            }

            return result.StandardOutput;
        }

        public void RunGitShow(string commit, string filePath, string outputFile)
        {
            // Ensure the file path is properly formatted for Git
            if (filePath.StartsWith("/"))
            {
                filePath = filePath.Substring(1); // Remove the leading slash if it exists
            }

            string quotedFilePath = $"\"{filePath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"show {commit}:{quotedFilePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _workingDirectory // Set the working directory
            };

            ProcessResult result;
            using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                result = _processWrapper.Start(startInfo, outputStream);
            }

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error running git show: {result.StandardError}");
            }
        }

        public string[] GetGitCommits(string earliest, string latest)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "log --pretty=format:%H",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _workingDirectory // Set the working directory
            };

            var result = _processWrapper.Start(startInfo);

            if (result.ExitCode != 0)
            {
                Console.WriteLine($"Error retrieving git commits: {result.StandardError}");
                return Array.Empty<string>();
            }

            var commits = result.StandardOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            return FilterCommitsByRange(commits, earliest, latest);
        }

        private string[] FilterCommitsByRange(string[] commits, string earliest, string latest)
        {
            int startIndex = 0;
            int endIndex = commits.Length - 1;

            // Find the index of the latest commit (should be closer to start of the array)
            if (!string.IsNullOrEmpty(latest))
            {
                startIndex = Array.IndexOf(commits, latest);
                if (startIndex == -1)
                {
                    Console.WriteLine($"Latest commit {latest} not found.");
                    return new string[0];
                }
            }

            // Find the index of the earliest commit (should be closer to the end of the array)
            if (!string.IsNullOrEmpty(earliest))
            {
                endIndex = Array.IndexOf(commits, earliest);
                if (endIndex == -1)
                {
                    Console.WriteLine($"Earliest commit {earliest} not found.");
                    return new string[0];
                }
            }

            // If the latest commit appears after the earliest commit in the list, the range is invalid
            if (startIndex > endIndex)
            {
                Console.WriteLine("Invalid commit range specified: latest commit is earlier than the earliest commit.");
                return new string[0];
            }

            return commits.Skip(startIndex).Take(endIndex - startIndex + 1).ToArray();
        }
    }
}
