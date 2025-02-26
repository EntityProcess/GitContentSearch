using LibGit2Sharp;

namespace GitContentSearch
{
    // Helper extension method to recursively traverse trees
    public static class TreeExtensions
    {
        public static IEnumerable<TreeEntry> RecursiveSelect(
            this Tree tree,
            Func<TreeEntry, TreeEntry> selector,
            Func<TreeEntry, Tree> recursion)
        {
            foreach (var item in tree)
            {
                if (item.TargetType == TreeEntryTargetType.Tree)
                {
                    var subtree = recursion(item);
                    foreach (var child in RecursiveSelect(subtree, selector, recursion))
                    {
                        yield return selector(child);
                    }
                }
                else
                {
                    yield return selector(item);
                }
            }
        }
    }

    public class GitLocator : IGitLocator
    {
        private readonly IGitHelper _gitHelper;
        private readonly TextWriter _logWriter;
        private readonly IProcessWrapper _processWrapper;

        public GitLocator(IGitHelper gitHelper, TextWriter? logWriter = null)
        {
            _gitHelper = gitHelper;
            _logWriter = logWriter ?? Console.Out;
            _processWrapper = new ProcessWrapper();
        }

        public (string? CommitHash, string? FilePath) LocateFile(string fileName)
        {
            if (!_gitHelper.IsValidRepository())
            {
                _logWriter.WriteLine("Error: Not a valid git repository.");
                return (null, null);
            }

            // Try command line approach as it's faster
            var result = LocateFileUsingGitCommand(fileName);
            if (result.CommitHash != null)
            {
                return result;
            }

			_logWriter.WriteLine($"File {fileName} not found.");
			return (null, null);
		}

        private (string? CommitHash, string? FilePath) LocateFileUsingGitCommand(string fileName)
        {
            try
            {
                string? currentCommit = null;
                string? foundPath = null;
                var foundException = new Exception("Found match");

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
                    _logWriter.WriteLine($"Found '{fileName}' in commit {currentCommit} ({commitTime})");
                    _logWriter.WriteLine($"Full path: {foundPath}");
                    return (currentCommit, foundPath);
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                _logWriter.WriteLine($"Warning: Command failed: {ex.Message}");
                return (null, null);
            }
        }
    }
} 