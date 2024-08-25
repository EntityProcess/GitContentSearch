using System.Diagnostics;

namespace GitContentSearch
{
    public class GitHelper : IGitHelper
    {
        private readonly IProcessWrapper _processWrapper;

        public GitHelper(IProcessWrapper processWrapper)
        {
            _processWrapper = processWrapper;
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
                CreateNoWindow = true
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
                CreateNoWindow = true
            };

            var result = _processWrapper.Start(startInfo);

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error running git show: {result.StandardError}");
            }

            File.WriteAllText(outputFile, result.StandardOutput);
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
                CreateNoWindow = true
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

            if (!string.IsNullOrEmpty(latest))
            {
                startIndex = Array.IndexOf(commits, latest);
                if (startIndex == -1)
                {
                    Console.WriteLine($"Latest commit {latest} not found.");
                    startIndex = 0;
                }
            }

            if (!string.IsNullOrEmpty(earliest))
            {
                endIndex = Array.IndexOf(commits, earliest);
                if (endIndex == -1)
                {
                    Console.WriteLine($"Earliest commit {earliest} not found.");
                    endIndex = commits.Length - 1;
                }
            }

            if (startIndex > endIndex)
            {
                Console.WriteLine("Invalid commit range specified.");
                return new string[0];
            }

            return commits.Skip(startIndex).Take(endIndex - startIndex + 1).ToArray();
        }
    }
}
