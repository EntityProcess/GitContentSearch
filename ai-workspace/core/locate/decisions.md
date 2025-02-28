# GitLocator Implementation Decisions

## LibGit2Sharp vs Git Command-line Approach

We investigated using LibGit2Sharp directly to locate files in the git history instead of using the git command-line. After thorough testing, we've decided to stick with the git command-line approach for the following reasons:

1. **Performance Issues**: The LibGit2Sharp approach requires recursively searching through all subdirectories in each commit's tree structure. This recursive traversal is significantly slower than the git command-line approach, especially for repositories with:
   - Deep directory structures
   - Large number of files
   - Long commit histories

2. **Git Command Efficiency**: The `git log --name-only` command is much more efficient because:
   - It flattens the directory structure, providing a simple list of all files in each commit
   - It's highly optimized at the C level in the git implementation
   - It avoids the overhead of recursive directory traversal

3. **Implementation Complexity**: The LibGit2Sharp approach requires additional code to handle:
   - Recursive directory traversal
   - Commit history traversal with cycle detection
   - Error handling for various edge cases

4. **Memory Usage**: The recursive approach with LibGit2Sharp can consume significantly more memory when processing large repositories.

## Attempted Implementation

We implemented and tested a LibGit2Sharp-based approach that:
1. Used a breadth-first search to traverse the commit history
2. Recursively searched through directory trees in each commit
3. Used a HashSet to avoid processing the same commit multiple times

Despite optimizations, this approach was still significantly slower than the git command-line approach for most repositories.

## Decision

We will continue using the git command-line approach (`LocateFileUsingGitCommand`) as it provides the best balance of:
- Performance
- Reliability
- Simplicity
- Memory efficiency

This decision should be revisited only if:
1. There are significant improvements to LibGit2Sharp's tree traversal performance
2. We encounter specific use cases where the git command-line approach is inadequate
3. We need to eliminate the git command-line dependency for some reason

## Code Reference

Below is the attempted LibGit2Sharp implementation that we decided not to use:

```csharp
using LibGit2Sharp;
using System.Collections.Concurrent;

namespace GitContentSearch
{
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

            // Try LibGit2Sharp approach first as it's more efficient
            var result = LocateFileUsingLibGit2Sharp(fileName);
            if (result.CommitHash != null)
            {
                return result;
            }

            // Fall back to command line approach if LibGit2Sharp fails
            result = LocateFileUsingGitCommand(fileName);
            if (result.CommitHash != null)
            {
                return result;
            }

            _logWriter.WriteLine($"File {fileName} not found.");
            return (null, null);
        }

        private (string? CommitHash, string? FilePath) LocateFileUsingLibGit2Sharp(string fileName)
        {
            try
            {
                string repoPath = _gitHelper.GetRepositoryPath();
                using var repo = new Repository(repoPath);
                
                // Get only the current checked out branch
                var currentBranch = repo.Head;
                if (currentBranch.Tip == null)
                {
                    _logWriter.WriteLine("Warning: Current branch has no commits.");
                    return (null, null);
                }
                
                // Use a HashSet to avoid processing the same commit multiple times
                var processedCommits = new HashSet<string>();
                
                // Start from the tip of the current branch
                var commitQueue = new Queue<LibGit2Sharp.Commit>();
                commitQueue.Enqueue(currentBranch.Tip);
                
                while (commitQueue.Count > 0)
                {
                    var commit = commitQueue.Dequeue();
                    
                    // Skip if we've already processed this commit
                    if (!processedCommits.Add(commit.Sha))
                        continue;
                    
                    // Check if the file exists in this commit by recursively searching the tree
                    var matchingEntry = FindFileInTree(commit.Tree, fileName);
                    if (matchingEntry != null)
                    {
                        var commitTime = commit.Author.When.ToString("yyyy-MM-dd HH:mm:ss");
                        _logWriter.WriteLine($"Found '{fileName}' in commit {commit.Sha} ({commitTime})");
                        _logWriter.WriteLine($"Full path: {matchingEntry.Path}");
                        return (commit.Sha, matchingEntry.Path);
                    }
                    
                    // Add parents to the queue
                    foreach (var parent in commit.Parents)
                    {
                        commitQueue.Enqueue(parent);
                    }
                }
                
                return (null, null);
            }
            catch (Exception ex)
            {
                _logWriter.WriteLine($"Warning: LibGit2Sharp search failed: {ex.Message}");
                return (null, null);
            }
        }

        private TreeEntry? FindFileInTree(Tree tree, string fileName)
        {
            // First check direct entries in this tree
            foreach (var entry in tree)
            {
                // Check if this entry matches the filename
                if (entry.Path.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }

                // If this is a subtree (directory), recursively search it
                if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    var subtree = (Tree)entry.Target;
                    var result = FindFileInTree(subtree, fileName);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
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
                        "log --name-only --pretty=format:%H", // Removed --all to only search current branch
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