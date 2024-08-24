using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitContentSearch
{
    public class GitHelper
    {
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

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new Exception("Failed to start git process.");
                }

                string output = process.StandardOutput.ReadToEnd().Trim();
                string error = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Error getting commit time: {error}");
                }

                return output;
            }
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
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new Exception("Failed to start git process.");
                }

                using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    process.StandardOutput.BaseStream.CopyTo(outputStream);
                }

                string error = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Error running git show: {error}");
                }
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
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Console.WriteLine("Failed to start git process.");
                    return Array.Empty<string>();
                }

                string output = process.StandardOutput.ReadToEnd().Trim();
                string error = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"Error retrieving git commits: {error}");
                    return Array.Empty<string>();
                }

                var commits = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                return FilterCommitsByRange(commits, earliest, latest);
            }
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
