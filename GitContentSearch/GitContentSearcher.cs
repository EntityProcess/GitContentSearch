using GitContentSearch.Helpers;
using GitContentSearch.Interfaces;
using System.IO;
using System.Threading;

namespace GitContentSearch
{
	public class GitContentSearcher : IGitContentSearcher
	{
		private readonly IGitHelper _gitHelper;
		private readonly IFileSearcher _fileSearcher;
		private readonly IFileManager _fileManager;
		private readonly ISearchLogger _logger;
		private IProgress<double>? _progress;
		private double _currentProgress = 0;

		public GitContentSearcher(IGitHelper gitHelper, IFileSearcher fileSearcher, IFileManager fileManager, ISearchLogger logger)
		{
			_gitHelper = gitHelper;
			_fileSearcher = fileSearcher;
			_fileManager = fileManager;
			_logger = logger;
		}

		private bool FileExistsInCurrentCommit(string filePath)
		{
			return FileExistsInCurrentCommit(filePath, CancellationToken.None);
		}

		private bool FileExistsInCurrentCommit(string filePath, CancellationToken cancellationToken)
		{
			try
			{
				if (!_gitHelper.IsValidRepository())
				{
					_logger.WriteLine("Warning: Not a valid git repository.");
					return false;
				}

				return _gitHelper.FileExistsAtCommit("HEAD", filePath, cancellationToken);
			}
			catch (Exception)
			{
				return false;
			}
		}

		public void SearchContent(string filePath, string searchString, string earliestCommit = "", string latestCommit = "", IProgress<double>? progress = null, CancellationToken cancellationToken = default)
		{
			_progress = progress;
			_currentProgress = 0;
			_progress?.Report(0.05); // Initial 5% progress to show activity

			if (!_gitHelper.IsValidRepository())
			{
				_logger.WriteLine("Error: Not a valid git repository.");
				_progress?.Report(1.0);
				return;
			}

			if (!string.IsNullOrEmpty(earliestCommit) && !_gitHelper.IsValidCommit(earliestCommit))
			{
				_logger.WriteLine($"Error: Invalid earliest commit hash: {earliestCommit}");
				_progress?.Report(1.0);
				return;
			}

			if (!string.IsNullOrEmpty(latestCommit) && !_gitHelper.IsValidCommit(latestCommit))
			{
				_logger.WriteLine($"Error: Invalid latest commit hash: {latestCommit}");
				_progress?.Report(1.0);
				return;
			}

			if (!FileExistsInCurrentCommit(filePath, cancellationToken))
			{
				_logger.WriteLine($"Warning: The file '{filePath}' does not exist in the current commit.");
				_logger.WriteLine("The search will not include commits where the file path was not found.");
				_logger.WriteLine(""); // Empty line
			}

			if (!string.IsNullOrEmpty(earliestCommit) && !string.IsNullOrEmpty(latestCommit))
			{
				// Get all commits to determine their relative order
				var allCommits = _gitHelper.GetGitCommits("", "", filePath);
				var earliestIndex = allCommits.FindIndex(c => c.CommitHash == earliestCommit);
				var latestIndex = allCommits.FindIndex(c => c.CommitHash == latestCommit);

				// Since git log returns newest commits first, if earliest commit has a lower index,
				// it means it's more recent than the latest commit
				if (earliestIndex != -1 && latestIndex != -1 && earliestIndex < latestIndex)
				{
					_logger.WriteLine("Error: The earliest commit is more recent than the latest commit.");
					_progress?.Report(1.0);
					return;
				}
			}

			// Get commits for the specified range
			var commits = _gitHelper.GetGitCommits(earliestCommit, latestCommit, filePath, cancellationToken);
			commits.Reverse();

			if (commits == null || commits.Count() == 0)
			{
				// If commits list is empty due to invalid order, the error message has already been logged by GitHelper
				// Otherwise, log that no commits were found
				if (string.IsNullOrEmpty(earliestCommit) || string.IsNullOrEmpty(latestCommit))
				{
					_logger.WriteLine("No commits found containing the specified file.");
				}
				_progress?.Report(1.0);
				return;
			}

			// Check commit order if both commits are specified
			if (!string.IsNullOrEmpty(earliestCommit) && !string.IsNullOrEmpty(latestCommit))
			{
				var earliestIndex = commits.FindIndex(c => c.CommitHash == earliestCommit);
				var latestIndex = commits.FindIndex(c => c.CommitHash == latestCommit);
				
				if (earliestIndex == -1)
				{
					_logger.WriteLine($"Error: Earliest commit {earliestCommit} not found.");
					_progress?.Report(1.0);
					return;
				}
				
				if (latestIndex == -1)
				{
					_logger.WriteLine($"Error: Latest commit {latestCommit} not found.");
					_progress?.Report(1.0);
					return;
				}
			}

			_progress?.Report(0.25); // Commits retrieved

			// Calculate total possible commits to search
			int totalPossibleSearches = commits.Count;
			int totalSearchesDone = 0;

			// Check for cancellation before starting search
			cancellationToken.ThrowIfCancellationRequested();

			// Search the most recent match first with FindLastMatchIndex
			int lastMatchIndex = FindLastMatchIndex(commits, filePath, searchString, 0, ref totalSearchesDone, totalPossibleSearches, cancellationToken);

			// Check for cancellation before continuing
			cancellationToken.ThrowIfCancellationRequested();

			// Pass lastMatchIndex to FindFirstMatchIndex to optimize the search range
			int firstMatchIndex = FindFirstMatchIndex(commits, filePath, searchString, lastMatchIndex, ref totalSearchesDone, totalPossibleSearches, cancellationToken);

			LogResults(firstMatchIndex, lastMatchIndex, commits, searchString);
			
			_progress?.Report(1.0);
		}

		public void SearchContentByDate(string filePath, string searchString, DateTime? startDate = null, DateTime? endDate = null, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
		{
			_progress = progress;
			_currentProgress = 0;
			_progress?.Report(0.05); // Initial 5% progress to show activity

			if (!_gitHelper.IsValidRepository())
			{
				_logger.WriteLine("Error: Not a valid git repository.");
				_progress?.Report(1.0);
				return;
			}

			if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
			{
				_logger.WriteLine($"Error: Start date ({startDate.Value:yyyy-MM-dd}) is later than end date ({endDate.Value:yyyy-MM-dd})");
				_progress?.Report(1.0);
				return;
			}

			if (!FileExistsInCurrentCommit(filePath, cancellationToken))
			{
				_logger.WriteLine($"Warning: The file '{filePath}' does not exist in the current commit.");
				_logger.WriteLine("The search will not include commits where the file path was not found.");
				_logger.WriteLine(""); // Empty line
			}

			// Get commits for the specified date range, following renames
			var commits = _gitHelper.GetGitCommitsByDate(startDate, endDate, filePath, cancellationToken);
			commits.Reverse(); // Reverse to get chronological order

			if (commits.Count == 0)
			{
				_logger.WriteLine("No commits found containing the specified file within the given date range.");
				_progress?.Report(1.0);
				return;
			}

			_progress?.Report(0.25); // Commits retrieved

			// Calculate total possible commits to search
			int totalPossibleSearches = commits.Count;
			int totalSearchesDone = 0;

			// Check for cancellation before starting search
			cancellationToken.ThrowIfCancellationRequested();

			// Search the most recent match first with FindLastMatchIndex
			int lastMatchIndex = FindLastMatchIndex(commits, filePath, searchString, 0, ref totalSearchesDone, totalPossibleSearches, cancellationToken);

			// Check for cancellation before continuing
			cancellationToken.ThrowIfCancellationRequested();

			// Pass lastMatchIndex to FindFirstMatchIndex to optimize the search range
			int firstMatchIndex = FindFirstMatchIndex(commits, filePath, searchString, lastMatchIndex, ref totalSearchesDone, totalPossibleSearches, cancellationToken);

			LogResults(firstMatchIndex, lastMatchIndex, commits, searchString);
			
			_progress?.Report(1.0);
		}

		private int FindFirstMatchIndex(List<Commit> commits, string filePath, string searchString, int lastMatchIndex, ref int totalSearchesDone, int totalPossibleSearches, CancellationToken cancellationToken)
		{
			int left = 0;
			int right = lastMatchIndex; // Use lastMatchIndex as the upper bound
			int? firstMatchIndex = null;

			while (left <= right)
			{
				cancellationToken.ThrowIfCancellationRequested();

				int mid = left + (right - left) / 2;
				var commit = commits[mid];
				bool found = false;

				try
				{
					using (var stream = _gitHelper.GetFileContentAtCommit(commit.CommitHash, commit.FilePath, cancellationToken))
					{
						found = _fileSearcher.SearchInStream(stream, searchString, Path.GetExtension(commit.FilePath));
					}

					string commitTime = _gitHelper.GetCommitTime(commit.CommitHash);
					_logger.WriteLine($"Checked commit: {commit.CommitHash} at {commitTime}, found: {found}");
					_logger.Flush();
				}
				catch (Exception ex)
				{
					_logger.WriteLine($"Error retrieving file at commit {commit.CommitHash}: {ex.Message}");
				}

				totalSearchesDone++;
				// Calculate progress between 62.5% and 100% based on total possible searches
				_currentProgress = 0.625 + ((double)totalSearchesDone / totalPossibleSearches * 0.375);
				_progress?.Report(_currentProgress);

				if (found)
				{
					firstMatchIndex = mid;
					right = mid - 1; // Continue searching to the left to find the first match
				}
				else
				{
					left = mid + 1; // Search to the right
				}
			}

			return firstMatchIndex ?? -1;
		}

		private int FindLastMatchIndex(List<Commit> commits, string filePath, string searchString, int searchStartIndex, ref int totalSearchesDone, int totalPossibleSearches, CancellationToken cancellationToken)
		{
			int left = searchStartIndex == -1 ? 0 : searchStartIndex;
			int right = commits.Count - 1;
			int? lastMatchIndex = null;

			while (left <= right)
			{
				cancellationToken.ThrowIfCancellationRequested();

				int mid = left + (right - left) / 2;
				var commit = commits[mid];
				bool found = false;

				try
				{
					using (var stream = _gitHelper.GetFileContentAtCommit(commit.CommitHash, commit.FilePath, cancellationToken))
					{
						found = _fileSearcher.SearchInStream(stream, searchString, Path.GetExtension(commit.FilePath));
					}

					string commitTime = _gitHelper.GetCommitTime(commit.CommitHash);
					_logger.WriteLine($"Checked commit: {commit.CommitHash} at {commitTime}, found: {found}");
					_logger.Flush();
				}
				catch (Exception ex)
				{
					_logger.WriteLine($"Error retrieving file at commit {commit.CommitHash}: {ex.Message}");
				}

				totalSearchesDone++;
				// Calculate progress between 25% and 62.5% based on total possible searches
				_currentProgress = 0.25 + ((double)totalSearchesDone / totalPossibleSearches * 0.375);
				_progress?.Report(_currentProgress);

				if (found)
				{
					lastMatchIndex = mid;
					left = mid + 1; // Continue searching to the right to find the last match
				}
				else
				{
					// If not found, check remaining commits with linear search
					int? linearSearchResult = PerformLinearSearch(commits, filePath, searchString, mid + 1, right, ref totalSearchesDone, totalPossibleSearches, cancellationToken, reverse: true);
					if (linearSearchResult.HasValue)
					{
						lastMatchIndex = linearSearchResult;
						break;
					}

					right = mid - 1; // Continue searching to the left
				}
			}

			return lastMatchIndex ?? -1;
		}

		private int? PerformLinearSearch(List<Commit> commits, string filePath, string searchString, int left, int right, ref int totalSearchesDone, int totalPossibleSearches, CancellationToken cancellationToken, bool reverse = false)
		{
			int step = reverse ? -1 : 1; // Use step to control direction of iteration
			int start = reverse ? right : left;
			int end = reverse ? left : right;

			for (int i = start; reverse ? i >= end : i <= end; i += step)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var commit = commits[i];
				bool found = false;

				try
				{
					using (var stream = _gitHelper.GetFileContentAtCommit(commit.CommitHash, commit.FilePath, cancellationToken))
					{
						found = _fileSearcher.SearchInStream(stream, searchString, Path.GetExtension(commit.FilePath));
					}

					string commitTime = _gitHelper.GetCommitTime(commit.CommitHash);
					_logger.WriteLine($"Checked commit: {commit.CommitHash} at {commitTime}, found: {found}");
					_logger.Flush();
				}
				catch (Exception ex)
				{
					_logger.WriteLine($"Error retrieving file at commit {commit.CommitHash}: {ex.Message}");
				}

				totalSearchesDone++;
				// Calculate progress based on total possible searches
				_currentProgress = 0.25 + ((double)totalSearchesDone / totalPossibleSearches * 0.375);
				_progress?.Report(_currentProgress);

				if (found)
				{
					return i; // Return the index as soon as a match is found
				}
			}

			return null; // Return null if no match is found
		}

		private void LogResults(int firstMatchIndex, int lastMatchIndex, List<Commit> commits, string searchString)
		{
			if (firstMatchIndex == -1)
			{
				_logger.WriteLine($"Search string \"{searchString}\" does not appear in any of the checked commits.");
			}
			else
			{
				_logger.WriteLine($"Search string \"{searchString}\" first appears in commit {commits[firstMatchIndex].CommitHash}.");
				if (lastMatchIndex != -1)
				{
					_logger.WriteLine($"Search string \"{searchString}\" last appears in commit {commits[lastMatchIndex].CommitHash}.");
					if (lastMatchIndex < commits.Count - 1)
					{
						_logger.WriteLine($"Search string \"{searchString}\" disappeared in commit {commits[lastMatchIndex + 1].CommitHash}.");
					}
				}
			}

			_logger.Flush();
		}
	}
}
