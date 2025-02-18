using System.Diagnostics;

namespace GitContentSearch
{
	public class GitHelper : IGitHelper
	{
		private readonly IProcessWrapper _processWrapper;
		private readonly string? _workingDirectory;
		private readonly bool _follow;

		public GitHelper(IProcessWrapper processWrapper)
		{
			_processWrapper = processWrapper;
		}

		public GitHelper(IProcessWrapper processWrapper, string? workingDirectory) 
			: this(processWrapper, workingDirectory, false)
		{
			
		}

		public GitHelper(IProcessWrapper processWrapper, string? workingDirectory, bool follow)
		{
			_processWrapper = processWrapper;
			_workingDirectory = workingDirectory;
			_follow = follow;
		}

		public string GetCommitTime(string commitHash)
		{
			var result = RunGitCommand($"show -s --format=%ci {commitHash}");

			if (result.ExitCode != 0)
			{
				throw new Exception($"Error getting commit time: {result.StandardError}");
			}

			return result.StandardOutput;
		}

		public void RunGitShow(string commit, string filePath, string outputFile)
		{
			string quotedFilePath = FormatFilePathForGit(filePath);

			ProcessResult result;
			using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
			{
				result = _processWrapper.Start($"show {commit}:{quotedFilePath}", _workingDirectory, outputStream);
			}

			if (result.ExitCode != 0)
			{
				throw new Exception($"Error running git show: {result.StandardError}");
			}
		}

		public List<Commit> GetGitCommits(string earliest, string latest)
		{
			return GetGitCommits(earliest, latest, string.Empty);
		}

		public List<Commit> GetGitCommits(string earliest, string latest, string filePath)
		{
			var mostRecentCommitHash = GetMostRecentCommitHash();
			var filteredCommits = _follow 
				? GetCommitsWithFollow(filePath) 
				: GetCommits(filePath);
			
			if (mostRecentCommitHash != null && !filteredCommits.Any(x => x.CommitHash == mostRecentCommitHash))
			{
				filteredCommits = new List<Commit> { new Commit(mostRecentCommitHash, filePath) }
					.Concat(filteredCommits)
					.ToList();
			}

			return FilterCommitsByRange(filteredCommits, earliest, latest);
		}

		private string? GetMostRecentCommitHash()
		{
			var result = RunGitCommand("log --pretty=format:%H -n 1");
			if (result == null || result.ExitCode != 0)
			{
				Console.WriteLine($"Error retrieving git commits: {result?.StandardError}");
				return null;
			}

			return result.StandardOutput.Trim();
		}

		private List<Commit> GetCommits(string filePath)
		{
			var filePathArg = string.IsNullOrEmpty(filePath) ? string.Empty : $"-- {FormatFilePathForGit(filePath)}";
			var arguments = $"log --pretty=format:%H {filePathArg}".Trim();
			var result = RunGitCommand(arguments);
			if (result == null || result.ExitCode != 0)
			{
				Console.WriteLine($"Error retrieving git commits: {result?.StandardError}");
				return new List<Commit>();
			}

			return result.StandardOutput
							 .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
							 .Select(x => new Commit(x, filePath))
							 .ToList();
		}

		private List<Commit> GetCommitsWithFollow(string filePath)
		{
			var filePathArg = string.IsNullOrEmpty(filePath) ? string.Empty : $"-- {FormatFilePathForGit(filePath)}";
			var arguments = $"log --name-status --pretty=format:%H --follow {filePathArg}".Trim();
			var result = RunGitCommand(arguments);
			if (result.ExitCode != 0)
			{
				Console.WriteLine($"Error retrieving git commits: {result?.StandardError}");
				return new List<Commit>();
			}

			var commitLines = result.StandardOutput
							 .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
							 .ToArray();

			var commits = new List<Commit>();
			string? currentCommitHash = null;
			string? currentFilePath = null;
			string? previousFilePath = filePath;

			foreach (var line in commitLines)
			{
				if (line.Length == 40 && line.All(c => char.IsLetterOrDigit(c)))
				{
					currentCommitHash = line;
				}
				else if (line.StartsWith("R") && currentCommitHash != null)
				{
					var parts = line.Split('\t');
					if (parts.Length == 3)
					{
						var oldPath = parts[1];
						currentFilePath = parts[2];
						if (oldPath != currentFilePath)
						{
							var commitTime = GetCommitTime(currentCommitHash);
							Console.WriteLine($"File renamed in commit {currentCommitHash} at {commitTime.Trim()}:");
							Console.WriteLine($"  From: {oldPath}");
							Console.WriteLine($"  To:   {currentFilePath}");
						}
						previousFilePath = currentFilePath;
						commits.Add(new Commit(currentCommitHash, currentFilePath));
					}
				}
				else if (currentCommitHash != null)
				{
					var parts = line.Split('\t');
					if (parts.Length == 2)
					{
						currentFilePath = parts[1];
						if (previousFilePath != currentFilePath)
						{
							var commitTime = GetCommitTime(currentCommitHash);
							Console.WriteLine($"File path changed in commit {currentCommitHash} at {commitTime.Trim()}:");
							Console.WriteLine($"  New path: {currentFilePath}");
							previousFilePath = currentFilePath;
						}
						commits.Add(new Commit(currentCommitHash, currentFilePath));
					}
				}
			}

			return commits;
		}

		private string FormatFilePathForGit(string filePath)
		{
			return filePath.StartsWith("/") ? $"\"{filePath.Substring(1)}\"" : $"\"{filePath}\"";
		}

		ProcessResult RunGitCommand(string arguments, Stream? outputStream = null)
		{
			return _processWrapper.Start(arguments, _workingDirectory, outputStream);
		}

		private List<Commit> FilterCommitsByRange(List<Commit> commits, string earliest, string latest)
		{
			int startIndex = 0;
			int endIndex = commits.Count - 1;

			// Find the index of the latest commit (should be closer to start of the list)
			if (!string.IsNullOrEmpty(latest))
			{
				startIndex = commits.FindIndex(c => c.CommitHash == latest);
				if (startIndex == -1)
				{
					Console.WriteLine($"Latest commit {latest} not found.");
					return new List<Commit>();
				}
			}

			// Find the index of the earliest commit (should be closer to the end of the list)
			if (!string.IsNullOrEmpty(earliest))
			{
				endIndex = commits.FindIndex(c => c.CommitHash == earliest);
				if (endIndex == -1)
				{
					Console.WriteLine($"Earliest commit {earliest} not found.");
					return new List<Commit>();
				}
			}

			// If the latest commit appears after the earliest commit in the list, the range is invalid
			if (startIndex > endIndex)
			{
				Console.WriteLine("Invalid commit range specified: latest commit is earlier than the earliest commit.");
				return new List<Commit>();
			}

			return commits.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
		}
	}
}
