using GitContentSearch.Interfaces;
using LibGit2Sharp;

namespace GitContentSearch
{
	public class GitHelper : IGitHelper, IDisposable
	{
		private readonly IProcessWrapper _processWrapper;
		private readonly string? _workingDirectory;
		private readonly bool _follow;
		private readonly ISearchLogger? _logger;
		private Repository? _repository;

		public GitHelper(IProcessWrapper processWrapper)
			: this(processWrapper, null, false, null)
		{
		}

		public GitHelper(IProcessWrapper processWrapper, string? workingDirectory) 
			: this(processWrapper, workingDirectory, false, null)
		{
		}

		public GitHelper(IProcessWrapper processWrapper, string? workingDirectory, bool follow)
			: this(processWrapper, workingDirectory, follow, null)
		{
		}

		public GitHelper(IProcessWrapper processWrapper, string? workingDirectory, bool follow, ISearchLogger? logger)
		{
			_processWrapper = processWrapper;
			_workingDirectory = workingDirectory;
			_follow = follow;
			_logger = logger;
			InitializeRepository();
		}

		private void InitializeRepository()
		{
			try
			{
				string repoPath = Repository.Discover(_workingDirectory ?? Directory.GetCurrentDirectory());
				if (!string.IsNullOrEmpty(repoPath))
				{
					_repository = new Repository(repoPath);
				}
			}
			catch (Exception ex)
			{
				_logger?.WriteLine($"Failed to initialize LibGit2Sharp repository: {ex.Message}");
			}
		}

		public string GetCommitTime(string commitHash)
		{
			EnsureRepositoryInitialized();

			var commit = _repository!.Lookup<LibGit2Sharp.Commit>(commitHash);
			if (commit == null)
			{
				throw new ArgumentException($"Invalid commit hash: {commitHash}");
			}

			return commit.Author.When.ToString("yyyy-MM-dd HH:mm:ss zzz");
		}

		public List<Commit> GetGitCommits(string earliestCommit, string latestCommit, string filePath = "")
		{
			return GetGitCommits(earliestCommit, latestCommit, filePath, CancellationToken.None);
		}

		public List<Commit> GetGitCommits(string earliestCommit, string latestCommit, string filePath, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			
			var mostRecentCommitHash = GetMostRecentCommitHash(cancellationToken);
			var filteredCommits = _follow 
				? GetCommitsWithFollow(filePath, cancellationToken) 
				: GetCommits(filePath, cancellationToken);
			
			if (mostRecentCommitHash != null && !filteredCommits.Any(x => x.CommitHash == mostRecentCommitHash))
			{
				filteredCommits = new List<Commit> { new Commit(mostRecentCommitHash, filePath) }
					.Concat(filteredCommits)
					.ToList();
			}

			return FilterCommitsByRange(filteredCommits, earliestCommit, latestCommit);
		}

		private string? GetMostRecentCommitHash()
		{
			return GetMostRecentCommitHash(CancellationToken.None);
		}

		private string? GetMostRecentCommitHash(CancellationToken cancellationToken)
		{
			var result = RunGitCommand("log --pretty=format:%H -n 1", null, cancellationToken);
			if (result == null || result.ExitCode != 0)
			{
				_logger?.WriteLine($"Error retrieving git commits: {result?.StandardError}");
				return null;
			}

			return result.StandardOutput.Trim();
		}

		private List<Commit> GetCommits(string filePath)
		{
			return GetCommits(filePath, CancellationToken.None);
		}

		private List<Commit> GetCommits(string filePath, CancellationToken cancellationToken)
		{
			var filePathArg = string.IsNullOrEmpty(filePath) ? string.Empty : $"-- {FormatFilePathForGit(filePath)}";
			var arguments = $"log --pretty=format:%H {filePathArg}".Trim();
			var result = RunGitCommand(arguments, null, cancellationToken);
			if (result == null || result.ExitCode != 0)
			{
				_logger?.WriteLine($"Error retrieving git commits: {result?.StandardError}");
				return new List<Commit>();
			}

			return result.StandardOutput
							 .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
							 .Select(x => new Commit(x, filePath))
							 .ToList();
		}

		private List<Commit> GetCommitsWithFollow(string filePath)
		{
			return GetCommitsWithFollow(filePath, CancellationToken.None);
		}

		private List<Commit> GetCommitsWithFollow(string filePath, CancellationToken cancellationToken)
		{
			var filePathArg = string.IsNullOrEmpty(filePath) ? string.Empty : $"-- {FormatFilePathForGit(filePath)}";
			var arguments = $"log --name-status --pretty=format:%H --follow {filePathArg}".Trim();
			var result = RunGitCommand(arguments, null, cancellationToken);
			if (result == null || result.ExitCode != 0)
			{
				_logger?.WriteLine($"Error retrieving git commits: {result?.StandardError}");
				return new List<Commit>();
			}

			return ParseGitLogWithNameStatus(result.StandardOutput, filePath, cancellationToken);
		}

		private List<Commit> ParseGitLogWithNameStatus(string gitLogOutput, string originalFilePath, CancellationToken cancellationToken)
		{
			var commitLines = gitLogOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
								 .ToArray();

			var commits = new List<Commit>();
			var processedCommits = new HashSet<string>(); // Track processed commits to avoid duplicates
			string? currentCommitHash = null;
			string? currentFilePath = null;
			string? previousFilePath = originalFilePath;

			foreach (var line in commitLines)
			{
				// Check for cancellation
				cancellationToken.ThrowIfCancellationRequested();
				
				if (line.Length == 40 && line.All(c => char.IsLetterOrDigit(c)))
				{
					currentCommitHash = line;
					// Skip if we've already processed this commit
					if (processedCommits.Contains(currentCommitHash))
					{
						currentCommitHash = null;
						continue;
					}
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
							_logger?.WriteLine($"File renamed in commit {currentCommitHash} at {commitTime.Trim()}:");
							_logger?.WriteLine($"  From: {oldPath}");
							_logger?.WriteLine($"  To:   {currentFilePath}");
						}
						previousFilePath = currentFilePath;
						commits.Add(new Commit(currentCommitHash, currentFilePath));
						processedCommits.Add(currentCommitHash);
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
							_logger?.WriteLine($"File path changed in commit {currentCommitHash} at {commitTime.Trim()}:");
							_logger?.WriteLine($"  New path: {currentFilePath}");
							previousFilePath = currentFilePath;
						}
						commits.Add(new Commit(currentCommitHash, currentFilePath));
						processedCommits.Add(currentCommitHash);
					}
				}
			}

			return commits;
		}

		private string FormatFilePathForGit(string filePath)
		{
			return filePath.StartsWith("/") ? $"\"{filePath[1..]}\"" : $"\"{filePath}\"";
		}

		ProcessResult RunGitCommand(string arguments, Stream? outputStream = null)
		{
			return RunGitCommand(arguments, outputStream, CancellationToken.None);
		}

		ProcessResult RunGitCommand(string arguments, Stream? outputStream, CancellationToken cancellationToken)
		{
			return _processWrapper.Start(arguments, _workingDirectory, outputStream, cancellationToken);
		}

		private List<Commit> FilterCommitsByRange(List<Commit> commits, string earliest, string latest)
		{
			int startIndex = 0;
			int endIndex = commits.Count - 1;

			// Find the index of the latest commit (should be closer to start of the list since git log returns newest first)
			if (!string.IsNullOrEmpty(latest))
			{
				startIndex = commits.FindIndex(c => c.CommitHash == latest);
				if (startIndex == -1)
				{
					_logger?.WriteLine($"Latest commit {latest} not found.");
					return new List<Commit>();
				}
			}

			// Find the index of the earliest commit (should be closer to end of the list since git log returns newest first)
			if (!string.IsNullOrEmpty(earliest))
			{
				endIndex = commits.FindIndex(c => c.CommitHash == earliest);
				if (endIndex == -1)
				{
					_logger?.WriteLine($"Earliest commit {earliest} not found.");
					return new List<Commit>();
				}
			}

			// Since git log returns commits in reverse chronological order (newest first),
			// the latest commit should have a lower index than the earliest commit
			if (startIndex > endIndex)
			{
				_logger?.WriteLine("Error: The earliest commit is more recent than the latest commit.");
				return new List<Commit>();
			}

			// Return the commits in the range, inclusive of both start and end
			return commits.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
		}

		public string GetRepositoryPath()
		{
			EnsureRepositoryInitialized();
			return _repository!.Info.WorkingDirectory ?? throw new InvalidOperationException("Repository working directory is null.");
		}

		public bool IsValidRepository()
		{
			return _repository != null;
		}

		public bool IsValidCommit(string commitHash)
		{
			if (!IsValidRepository()) return false;
			
			try
			{
				var commit = _repository!.Lookup<LibGit2Sharp.Commit>(commitHash);
				return commit != null;
			}
			catch
			{
				return false;
			}
		}

		public Stream GetFileContentAtCommit(string commitHash, string filePath)
		{
			return GetFileContentAtCommit(commitHash, filePath, CancellationToken.None);
		}

		public Stream GetFileContentAtCommit(string commitHash, string filePath, CancellationToken cancellationToken)
		{
			EnsureRepositoryInitialized();
			
			cancellationToken.ThrowIfCancellationRequested();
			
			var commit = _repository!.Lookup<LibGit2Sharp.Commit>(commitHash);
			if (commit == null)
			{
				throw new ArgumentException($"Invalid commit hash: {commitHash}");
			}

			var tree = commit.Tree;
			var treeEntry = tree[filePath];
			if (treeEntry == null)
			{
				throw new FileNotFoundException($"File {filePath} not found in commit {commitHash}");
			}

			var blob = (Blob)treeEntry.Target;
			return blob.GetContentStream();
		}

		public Dictionary<string, string> GetFileHistory(string filePath)
		{
			EnsureRepositoryInitialized();
			
			var history = new Dictionary<string, string>();
			var filter = new CommitFilter
			{
				SortBy = CommitSortStrategies.Time,
				IncludeReachableFrom = _repository!.Head
			};

			foreach (var commit in _repository.Commits.QueryBy(filter))
			{
				try
				{
					var tree = commit.Tree;
					var entry = tree[filePath];
					if (entry != null)
					{
						history[commit.Sha] = commit.MessageShort;
					}
				}
				catch
				{
					// Skip if file doesn't exist in this commit
					continue;
				}
			}

			return history;
		}

		public List<(string Hash, string Message, DateTimeOffset When)> GetCommitLog(int maxCount = 100)
		{
			EnsureRepositoryInitialized();
			
			return _repository!.Commits
				.Take(maxCount)
				.Select(c => (c.Sha, c.MessageShort, c.Author.When))
				.ToList();
		}

		public List<Commit> GetGitCommitsByDate(DateTime? startDate, DateTime? endDate, string filePath = "")
		{
			return GetGitCommitsByDate(startDate, endDate, filePath, CancellationToken.None);
		}

		public List<Commit> GetGitCommitsByDate(DateTime? startDate, DateTime? endDate, string filePath, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var filePathArg = string.IsNullOrEmpty(filePath) ? string.Empty : $"-- {FormatFilePathForGit(filePath)}";
			var startDateArg = startDate.HasValue ? $"--since=\"{startDate.Value:yyyy-MM-dd}\"" : string.Empty;
			var endDateArg = endDate.HasValue ? $"--until=\"{endDate.Value:yyyy-MM-dd}\"" : string.Empty;

			if (_follow && !string.IsNullOrEmpty(filePath))
			{
				// When following file renames, we need to use --name-status to track the file path changes
				var arguments = $"log --name-status --pretty=format:%H --follow {startDateArg} {endDateArg} {filePathArg}".Trim();
				var result = RunGitCommand(arguments, null, cancellationToken);
				if (result == null || result.ExitCode != 0)
				{
					_logger?.WriteLine($"Error retrieving git commits: {result?.StandardError}");
					return new List<Commit>();
				}

				return ParseGitLogWithNameStatus(result.StandardOutput, filePath, cancellationToken);
			}
			else
			{
				// When not following renames, use the simpler log command
				var arguments = $"log --pretty=format:%H {startDateArg} {endDateArg} {filePathArg}".Trim();
				var result = RunGitCommand(arguments, null, cancellationToken);
				if (result == null || result.ExitCode != 0)
				{
					_logger?.WriteLine($"Error retrieving git commits: {result?.StandardError}");
					return new List<Commit>();
				}

				var commits = result.StandardOutput
								 .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
								 .Select(x => new Commit(x, filePath))
								 .ToList();

				if (!commits.Any())
				{
					_logger?.WriteLine($"No commits found between {startDate?.ToString("yyyy-MM-dd") ?? "repository start"} and {endDate?.ToString("yyyy-MM-dd") ?? "repository end"}");
				}

				return commits;
			}
		}

		private void EnsureRepositoryInitialized()
		{
			if (_repository == null)
			{
				throw new InvalidOperationException("Git repository is not initialized or is invalid.");
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_repository?.Dispose();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
