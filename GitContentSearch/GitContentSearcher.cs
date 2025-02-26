using GitContentSearch.Helpers;

namespace GitContentSearch
{
	public class GitContentSearcher : IGitContentSearcher
	{
		private readonly IGitHelper _gitHelper;
		private readonly IFileSearcher _fileSearcher;
		private readonly IFileManager _fileManager;
		private readonly TextWriter _logWriter;
		private readonly bool _disableLinearSearch;
		private IProgress<double>? _progress;
		private double _currentProgress = 0;

		public GitContentSearcher(IGitHelper gitHelper, IFileSearcher fileSearcher, IFileManager fileManager, bool disableLinearSearch, TextWriter? logWriter = null)
		{
			_logWriter = logWriter ?? new CompositeTextWriter(
				Console.Out,
				new StreamWriter("search_log.txt", append: true)
			);
			_gitHelper = gitHelper;
			_fileSearcher = fileSearcher;
			_fileManager = fileManager;
			_disableLinearSearch = disableLinearSearch;
		}

		private bool FileExistsInCurrentCommit(string filePath)
		{
			try
			{
				if (!_gitHelper.IsValidRepository())
				{
					_logWriter.WriteLine("Warning: Not a valid git repository.");
					return false;
				}

				// Use LibGit2Sharp to check file existence
				var content = _gitHelper.GetFileContentAtCommit("HEAD", filePath);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public void SearchContent(string filePath, string searchString, string earliestCommit = "", string latestCommit = "", IProgress<double>? progress = null)
		{
			_progress = progress;
			_currentProgress = 0;
			_progress?.Report(0.05); // Initial 5% progress to show activity

			if (!_gitHelper.IsValidRepository())
			{
				_logWriter.WriteLine("Error: Not a valid git repository.");
				_progress?.Report(1.0);
				return;
			}

			if (!string.IsNullOrEmpty(earliestCommit) && !_gitHelper.IsValidCommit(earliestCommit))
			{
				_logWriter.WriteLine($"Error: Invalid earliest commit hash: {earliestCommit}");
				_progress?.Report(1.0);
				return;
			}

			if (!string.IsNullOrEmpty(latestCommit) && !_gitHelper.IsValidCommit(latestCommit))
			{
				_logWriter.WriteLine($"Error: Invalid latest commit hash: {latestCommit}");
				_progress?.Report(1.0);
				return;
			}

			if (!FileExistsInCurrentCommit(filePath))
			{
				_logWriter.WriteLine($"Warning: The file '{filePath}' does not exist in the current commit.");
				_logWriter.WriteLine("The search will not include commits where the file path was not found.");
				_logWriter.WriteLine();
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
					_logWriter.WriteLine("Error: The earliest commit is more recent than the latest commit.");
					_progress?.Report(1.0);
					return;
				}
			}

			// Get commits for the specified range
			var commits = _gitHelper.GetGitCommits(earliestCommit, latestCommit, filePath);
			commits.Reverse();

			if (commits == null || commits.Count == 0)
			{
				// If commits list is empty due to invalid order, the error message has already been logged by GitHelper
				// Otherwise, log that no commits were found
				if (string.IsNullOrEmpty(earliestCommit) || string.IsNullOrEmpty(latestCommit))
				{
					_logWriter.WriteLine("No commits found containing the specified file.");
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
					_logWriter.WriteLine($"Error: Earliest commit {earliestCommit} not found.");
					_progress?.Report(1.0);
					return;
				}
				
				if (latestIndex == -1)
				{
					_logWriter.WriteLine($"Error: Latest commit {latestCommit} not found.");
					_progress?.Report(1.0);
					return;
				}
			}

			_progress?.Report(0.25); // Commits retrieved

			// Calculate total possible commits to search
			int totalPossibleSearches = commits.Count;
			int totalSearchesDone = 0;

			// Search the most recent match first with FindLastMatchIndex
			int lastMatchIndex = FindLastMatchIndex(commits, filePath, searchString, 0, ref totalSearchesDone, totalPossibleSearches);

			// Pass lastMatchIndex to FindFirstMatchIndex to optimize the search range
			int firstMatchIndex = FindFirstMatchIndex(commits, filePath, searchString, lastMatchIndex, ref totalSearchesDone, totalPossibleSearches);

			LogResults(firstMatchIndex, lastMatchIndex, commits, searchString);
			
			_progress?.Report(1.0);
		}

		private int FindFirstMatchIndex(List<Commit> commits, string filePath, string searchString, int lastMatchIndex, ref int totalSearchesDone, int totalPossibleSearches)
		{
			int left = 0;
			int right = lastMatchIndex; // Use lastMatchIndex as the upper bound
			int? firstMatchIndex = null;

			while (left <= right)
			{
				int mid = left + (right - left) / 2;
				var commit = commits[mid];
				bool found = false;

				try
				{
					using var stream = _gitHelper.GetFileContentAtCommit(commit.CommitHash, commit.FilePath);
					bool isBinary = !_fileSearcher.IsTextStream(stream);
					found = _fileSearcher.SearchInStream(stream, searchString, isBinary);

					string commitTime = _gitHelper.GetCommitTime(commit.CommitHash);
					_logWriter.WriteLine($"Checked commit: {commit.CommitHash} at {commitTime}, found: {found}");
					_logWriter.Flush();
				}
				catch (Exception ex)
				{
					_logWriter.WriteLine($"Error retrieving file at commit {commit.CommitHash}: {ex.Message}");
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

		private int FindLastMatchIndex(List<Commit> commits, string filePath, string searchString, int searchStartIndex, ref int totalSearchesDone, int totalPossibleSearches)
		{
			int left = searchStartIndex == -1 ? 0 : searchStartIndex;
			int right = commits.Count - 1;
			int? lastMatchIndex = null;

			while (left <= right)
			{
				int mid = left + (right - left) / 2;
				var commit = commits[mid];
				bool found = false;

				try
				{
					using var stream = _gitHelper.GetFileContentAtCommit(commit.CommitHash, commit.FilePath);
					bool isBinary = !_fileSearcher.IsTextStream(stream);
					found = _fileSearcher.SearchInStream(stream, searchString, isBinary);

					string commitTime = _gitHelper.GetCommitTime(commit.CommitHash);
					_logWriter.WriteLine($"Checked commit: {commit.CommitHash} at {commitTime}, found: {found}");
					_logWriter.Flush();
				}
				catch (Exception ex)
				{
					_logWriter.WriteLine($"Error retrieving file at commit {commit.CommitHash}: {ex.Message}");
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
					// If not found and linear search is enabled, check remaining commits with linear search
					if (!_disableLinearSearch)
					{
						int? linearSearchResult = PerformLinearSearch(commits, filePath, searchString, mid + 1, right, ref totalSearchesDone, totalPossibleSearches, reverse: true);
						if (linearSearchResult.HasValue)
						{
							lastMatchIndex = linearSearchResult;
							break;
						}
					}

					right = mid - 1; // Continue searching to the left
				}
			}

			return lastMatchIndex ?? -1;
		}

		private int? PerformLinearSearch(List<Commit> commits, string filePath, string searchString, int left, int right, ref int totalSearchesDone, int totalPossibleSearches, bool reverse = false)
		{
			int step = reverse ? -1 : 1; // Use step to control direction of iteration
			int start = reverse ? right : left;
			int end = reverse ? left : right;

			for (int i = start; reverse ? i >= end : i <= end; i += step)
			{
				var commit = commits[i];
				bool found = false;

				try
				{
					using var stream = _gitHelper.GetFileContentAtCommit(commit.CommitHash, commit.FilePath);
					bool isBinary = !_fileSearcher.IsTextStream(stream);
					found = _fileSearcher.SearchInStream(stream, searchString, isBinary);

					string commitTime = _gitHelper.GetCommitTime(commit.CommitHash);
					_logWriter.WriteLine($"Checked commit: {commit.CommitHash} at {commitTime}, found: {found}");
					_logWriter.Flush();
				}
				catch (Exception ex)
				{
					_logWriter.WriteLine($"Error retrieving file at commit {commit.CommitHash}: {ex.Message}");
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
				_logWriter.WriteLine($"Search string \"{searchString}\" does not appear in any of the checked commits.");
			}
			else
			{
				_logWriter.WriteLine($"Search string \"{searchString}\" first appears in commit {commits[firstMatchIndex].CommitHash}.");
				if (lastMatchIndex != -1)
				{
					_logWriter.WriteLine($"Search string \"{searchString}\" last appears in commit {commits[lastMatchIndex].CommitHash}.");
					if (lastMatchIndex < commits.Count - 1)
					{
						_logWriter.WriteLine($"Search string \"{searchString}\" disappeared in commit {commits[lastMatchIndex + 1].CommitHash}.");
					}
				}
			}

			_logWriter.Flush();
		}
	}
}
