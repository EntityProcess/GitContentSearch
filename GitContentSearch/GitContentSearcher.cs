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

        public GitContentSearcher(IGitHelper gitHelper, IFileSearcher fileSearcher, IFileManager fileManager, bool disableLinearSearch, TextWriter? logWriter = null)
        {
            _gitHelper = gitHelper;
            _fileSearcher = fileSearcher;
            _fileManager = fileManager;
            _disableLinearSearch = disableLinearSearch;
            _logWriter = logWriter ?? new CompositeTextWriter(
                Console.Out,
                new StreamWriter("search_log.txt", append: true)
            );
        }

        public void SearchContent(string filePath, string searchString, string earliestCommit = "", string latestCommit = "")
        {
            var commits = _gitHelper.GetGitCommits(earliestCommit, latestCommit, filePath);
            commits = commits.Reverse().ToArray();

            if (commits == null || commits.Length == 0)
            {
                _logWriter.WriteLine("No commits found in the specified range.");
                return;
            }

            if (Array.IndexOf(commits, earliestCommit) > Array.IndexOf(commits, latestCommit))
            {
                _logWriter.WriteLine("Error: The earliest commit is more recent than the latest commit.");
                return;
            }

            int firstMatchIndex = FindFirstMatchIndex(commits, filePath, searchString);
            int lastMatchIndex = FindLastMatchIndex(commits, filePath, searchString, firstMatchIndex);

            LogResults(firstMatchIndex, lastMatchIndex, commits, searchString);
        }

        private int FindFirstMatchIndex(string[] commits, string filePath, string searchString)
        {
            int left = 0;
            int right = commits.Length - 1;
            int? firstMatchIndex = null;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                string commit = commits[mid];
                string tempFileName = _fileManager.GenerateTempFileName(commit, filePath);
                string commitTime = GetCommitTime(commit);

                bool gitShowSuccess = false;
                try
                {
                    _gitHelper.RunGitShow(commit, filePath, tempFileName);
                    gitShowSuccess = true;
                }
                catch (Exception ex)
                {
                    _logWriter.WriteLine($"Error retrieving file at commit {commit}: {ex.Message}");
                }

                bool found = gitShowSuccess && _fileSearcher.SearchInFile(tempFileName, searchString);

                _logWriter.WriteLine($"Checked commit: {commit} at {commitTime}, found: {found}");
                _logWriter.Flush();

                if (found)
                {
                    firstMatchIndex = mid;
                    right = mid - 1; // Continue searching to the left to find the first match
                }
                else
                {
                    if (!_disableLinearSearch)
                    {
                        // Use the linear search helper
                        int? linearSearchResult = PerformLinearSearch(commits, filePath, searchString, left, mid - 1);
                        if (linearSearchResult.HasValue)
                        {
                            firstMatchIndex = linearSearchResult;
                            break;
                        }
                    }

                    left = mid + 1;
                }

                _fileManager.DeleteTempFile(tempFileName);
            }

            return firstMatchIndex ?? -1;
        }

        private int FindLastMatchIndex(string[] commits, string filePath, string searchString, int searchStartIndex)
        {
            int left = searchStartIndex == -1 ? 0 : searchStartIndex;
            int right = commits.Length - 1;
            int? lastMatchIndex = null;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                string commit = commits[mid];
                string tempFileName = _fileManager.GenerateTempFileName(commit, filePath);
                string commitTime = GetCommitTime(commit);

                bool gitShowSuccess = false;
                try
                {
                    _gitHelper.RunGitShow(commit, filePath, tempFileName);
                    gitShowSuccess = true;
                }
                catch (Exception ex)
                {
                    _logWriter.WriteLine($"Error retrieving file at commit {commit}: {ex.Message}");
                }

                bool found = gitShowSuccess && _fileSearcher.SearchInFile(tempFileName, searchString);

                _logWriter.WriteLine($"Checked commit: {commit} at {commitTime}, found: {found}");
                _logWriter.Flush();

                if (found)
                {
                    lastMatchIndex = mid;
                    left = mid + 1; // Continue searching to the right to find the last match
                }
                else
                {
                    right = mid - 1;
                }

                _fileManager.DeleteTempFile(tempFileName);
            }

            return lastMatchIndex ?? -1;
        }

        private int? PerformLinearSearch(string[] commits, string filePath, string searchString, int left, int right)
        {
            int? matchIndex = null;

            for (int i = left; i <= right; i++)
            {
                string commit = commits[i];
                string tempFileName = _fileManager.GenerateTempFileName(commit, filePath);
                string commitTime = GetCommitTime(commit);

                bool gitShowSuccess = false;
                try
                {
                    _gitHelper.RunGitShow(commit, filePath, tempFileName);
                    gitShowSuccess = true;
                }
                catch (Exception ex)
                {
                    _logWriter.WriteLine($"Error retrieving file at commit {commit}: {ex.Message}");
                }

                bool found = gitShowSuccess && _fileSearcher.SearchInFile(tempFileName, searchString);

                _logWriter.WriteLine($"Linear search commit: {commit} at {commitTime}, found: {found}");
                _logWriter.Flush();

                if (found)
                {
                    matchIndex = i; // Update matchIndex when a match is found
                    break; // Stop search on first match (can be modified for last match)
                }

                _fileManager.DeleteTempFile(tempFileName);
            }

            return matchIndex;
        }

        private string GetCommitTime(string commit)
        {
            try
            {
                return _gitHelper.GetCommitTime(commit);
            }
            catch (Exception ex)
            {
                _logWriter.WriteLine($"Error retrieving commit time for {commit}: {ex.Message}");
                return "unknown time";
            }
        }

        private void LogResults(int firstMatchIndex, int lastMatchIndex, string[] commits, string searchString)
        {
            if (firstMatchIndex == -1)
            {
                _logWriter.WriteLine($"Search string \"{searchString}\" does not appear in any of the checked commits.");
            }
            else
            {
                _logWriter.WriteLine($"Search string \"{searchString}\" first appears in commit {commits[firstMatchIndex]}.");
                if (lastMatchIndex != -1)
                {
                    _logWriter.WriteLine($"Search string \"{searchString}\" last appears in commit {commits[lastMatchIndex]}.");
                }
            }

            _logWriter.Flush();
        }
    }
}
