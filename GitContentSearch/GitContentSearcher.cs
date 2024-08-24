namespace GitContentSearch
{
    public class GitContentSearcher
    {
        private readonly IGitHelper _gitHelper;
        private readonly IFileSearcher _fileSearcher;

        public GitContentSearcher()
        {
            _gitHelper = new GitHelper();
            _fileSearcher = new FileSearcher();
        }

        public GitContentSearcher(IGitHelper gitHelper, IFileSearcher fileSearcher)
        {
            _gitHelper = gitHelper;
            _fileSearcher = fileSearcher;
        }

        public void SearchContent(string filePath, string searchString, string earliestCommit = "", string latestCommit = "", TextWriter? logWriter = null)
        {
            // If no logWriter is provided, log to both console and file
            if (logWriter == null)
            {
                logWriter = new CompositeTextWriter(
                    Console.Out,
                    new StreamWriter("search_log.txt", append: true)
                );
            }

            var commits = _gitHelper.GetGitCommits(earliestCommit, latestCommit);

            if (commits == null || commits.Length == 0)
            {
                logWriter.WriteLine("No commits found in the specified range.");
                return;
            }

            int left = 0;
            int right = commits.Length - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                string commit = commits[mid];

                string tempFileName = $"temp_{commit}{Path.GetExtension(filePath)}";

                try
                {
                    _gitHelper.RunGitShow(commit, filePath, tempFileName);
                }
                catch (Exception ex)
                {
                    logWriter.WriteLine($"Error retrieving file at commit {commit}: {ex.Message}");
                    right = mid - 1;
                    continue;
                }

                bool found = _fileSearcher.SearchInFile(tempFileName, searchString);

                string commitTime;
                try
                {
                    commitTime = _gitHelper.GetCommitTime(commit);
                }
                catch (Exception ex)
                {
                    commitTime = $"unknown time ({ex.Message})";
                }

                logWriter.WriteLine($"Checked commit: {commit} at {commitTime}, found: {found}");
                logWriter.Flush();

                if (found)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }

                if (File.Exists(tempFileName))
                {
                    File.Delete(tempFileName);
                }
            }

            if (right < 0)
            {
                logWriter.WriteLine($"Search string \"{searchString}\" does not appear in any of the checked commits.");
            }
            else if (left >= commits.Length)
            {
                logWriter.WriteLine($"Search string \"{searchString}\" appears in all checked commits.");
            }
            else
            {
                logWriter.WriteLine($"Search string \"{searchString}\" appears in commit {commits[right]}.");
            }
        }
    }
}
