using GitContentSearch.Helpers;
using System;
using System.IO;
using System.Linq;

namespace GitContentSearch
{
    public interface IGitContentSearcher
    {
        void SearchContent(string filePath, string searchString, string earliestCommit = "", string latestCommit = "", TextWriter? logWriter = null);
    }

    public class GitContentSearcher : IGitContentSearcher
    {
        private readonly IGitHelper _gitHelper;
        private readonly IFileSearcher _fileSearcher;
        private readonly IFileManager _fileManager;

        public GitContentSearcher(IGitHelper gitHelper, IFileSearcher fileSearcher, IFileManager fileManager)
        {
            _gitHelper = gitHelper;
            _fileSearcher = fileSearcher;
            _fileManager = fileManager;
        }

        public void SearchContent(string filePath, string searchString, string earliestCommit = "", string latestCommit = "", TextWriter? logWriter = null)
        {
            logWriter ??= new CompositeTextWriter(
                Console.Out,
                new StreamWriter("search_log.txt", append: true)
            );

            var commits = _gitHelper.GetGitCommits(earliestCommit, latestCommit);

            if (commits == null || commits.Length == 0)
            {
                logWriter.WriteLine("No commits found in the specified range.");
                return;
            }

            int firstAppearanceIndex = FindAppearanceIndex(commits, filePath, searchString, true, logWriter);
            int lastAppearanceIndex = FindAppearanceIndex(commits, filePath, searchString, false, logWriter, firstAppearanceIndex);

            LogResults(firstAppearanceIndex, lastAppearanceIndex, commits, searchString, logWriter);
        }

        private int FindAppearanceIndex(string[] commits, string filePath, string searchString, bool isFirstSearch, TextWriter logWriter, int searchEndIndex = -1)
        {
            int left = isFirstSearch ? 0 : searchEndIndex == -1 ? 0 : searchEndIndex;
            int right = commits.Length - 1;
            int? appearanceIndex = null;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                string commit = commits[mid];
                string tempFileName = _fileManager.GenerateTempFileName(commit, filePath);

                try
                {
                    _gitHelper.RunGitShow(commit, filePath, tempFileName);
                }
                catch (Exception ex)
                {
                    logWriter.WriteLine($"Error retrieving file at commit {commit}: {ex.Message}");
                    if (isFirstSearch)
                    {
                        right = mid - 1;
                    }
                    else
                    {
                        left = mid + 1;
                    }
                    continue;
                }

                bool found = _fileSearcher.SearchInFile(tempFileName, searchString);
                string commitTime = GetCommitTime(commit, logWriter);

                logWriter.WriteLine($"Checked commit: {commit} at {commitTime}, found: {found}");
                logWriter.Flush();

                if (found)
                {
                    appearanceIndex = mid;
                    if (isFirstSearch)
                    {
                        right = mid - 1; // Continue searching to the left to find the first appearance
                    }
                    else
                    {
                        left = mid + 1; // Continue searching to the right to find the last appearance
                    }
                }
                else
                {
                    if (isFirstSearch)
                    {
                        left = mid + 1;
                    }
                    else
                    {
                        right = mid - 1;
                    }
                }

                _fileManager.DeleteTempFile(tempFileName);
            }

            return appearanceIndex ?? -1;
        }

        private string GetCommitTime(string commit, TextWriter logWriter)
        {
            try
            {
                return _gitHelper.GetCommitTime(commit);
            }
            catch (Exception ex)
            {
                logWriter.WriteLine($"Error retrieving commit time for {commit}: {ex.Message}");
                return "unknown time";
            }
        }

        private void LogResults(int firstAppearanceIndex, int lastAppearanceIndex, string[] commits, string searchString, TextWriter logWriter)
        {
            if (firstAppearanceIndex == -1)
            {
                logWriter.WriteLine($"Search string \"{searchString}\" does not appear in any of the checked commits.");
            }
            else
            {
                logWriter.WriteLine($"Search string \"{searchString}\" first appears in commit {commits[firstAppearanceIndex]}.");
                if (lastAppearanceIndex != -1)
                {
                    logWriter.WriteLine($"Search string \"{searchString}\" last appears in commit {commits[lastAppearanceIndex]}.");
                }
            }
        }
    }
}