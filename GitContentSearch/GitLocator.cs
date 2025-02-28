using GitContentSearch.Interfaces;
using LibGit2Sharp;

namespace GitContentSearch
{
    public class GitLocator : IGitLocator
    {
        private readonly IGitHelper _gitHelper;
        private readonly ISearchLogger _logger;
        private readonly IProcessWrapper _processWrapper;
        private const int PROGRESS_UPDATE_INTERVAL = 1000; // Update progress every 1000 commits

        public GitLocator(IGitHelper gitHelper, ISearchLogger logger, IProcessWrapper processWrapper)
        {
            _gitHelper = gitHelper;
            _logger = logger;
            _processWrapper = processWrapper;
        }

        public (string? CommitHash, string? FilePath) LocateFile(string fileName, IProgress<double>? progress = null)
        {
            if (!_gitHelper.IsValidRepository())
            {
                _logger.LogError("Not a valid git repository.");
                progress?.Report(1.0); // Report completion even on error
                return (null, null);
            }

            // Try command line approach as it's faster
            var result = LocateFileUsingGitCommand(fileName, progress);
            if (result.CommitHash != null)
            {
                progress?.Report(1.0); // Ensure we report completion
                return result;
            }

            _logger.WriteLine($"\nFile {fileName} not found.");
            progress?.Report(1.0); // Report completion when file not found
            return (null, null);
        }

        private (string? CommitHash, string? FilePath) LocateFileUsingGitCommand(string fileName, IProgress<double>? progress = null)
        {
            try
            {
                string? currentCommit = null;
                string? foundPath = null;
                var foundException = new Exception("Found match");
                int commitCount = 0;

                // Process output line by line and stop as soon as we find a match
                try
                {
                    _processWrapper.StartAndProcessOutput(
                        "log --all --name-only --pretty=format:%H",
                        _gitHelper.GetRepositoryPath(),
                        line =>
                        {
                            if (foundPath != null) 
                            {
                                throw foundException; // Break out of processing
                            }

                            if (line.Length == 40 && line.All(c => char.IsLetterOrDigit(c)))
                            {
                                currentCommit = line;
                                commitCount++;
                                if (commitCount % PROGRESS_UPDATE_INTERVAL == 0)
                                {
                                    _logger.LogProgress($"Processing commits: {commitCount}");
                                    // Report approximate progress (assuming most repos have less than 100k commits)
                                    progress?.Report(Math.Min(0.95, commitCount / 100000.0));
                                }
                            }
                            else if (currentCommit != null && line.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                            {
                                foundPath = line;
                                throw foundException; // Break out immediately when found
                            }
                        }
                    );
                }
                catch (Exception ex) when (ex == foundException)
                {
                    // Expected exception when match is found
                }

                if (currentCommit != null && foundPath != null)
                {
                    var commitTime = _gitHelper.GetCommitTime(currentCommit);
                    _logger.WriteLine($"Found '{fileName}' in commit {currentCommit} ({commitTime})");
                    _logger.WriteLine($"Full path: {foundPath}");
                    return (currentCommit, foundPath);
                }

                _logger.LogFooter();
                _logger.WriteLine($"Processed {commitCount} commits without finding the file.");
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError("Command failed", ex);
                return (null, null);
            }
        }
    }
} 