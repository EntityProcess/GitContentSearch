using GitContentSearch.Interfaces;
using System.Threading;

namespace GitContentSearch
{
    /// <summary>
    /// Locates files in a Git repository's history using an efficient binary search approach combined with rename tracking.
    /// 
    /// Algorithm Overview:
    /// 1. Binary Search Phase:
    ///    - Gets a list of all commit hashes in chronological order
    ///    - Uses binary search to efficiently find any occurrence of the target file
    ///    - For each checked commit, uses 'git ls-tree' to list files and find matches
    ///    - Displays the file path when found and when it changes between commits
    /// 
    /// 2. Rename Tracking Phase:
    ///    - Once the file is found in a commit, tracks its history forward to HEAD
    ///    - Uses 'git log --follow --name-status' to detect renames and deletions
    ///    - Maintains a chronological history of all paths the file has had
    ///    - Returns the most recent valid path and commit where the file exists
    /// 
    /// Performance Considerations:
    /// - Uses binary search to quickly find first occurrence instead of scanning all commits
    /// - Only tracks renames forward from first found commit, not entire history
    /// - Caches commit times and reuses them to minimize git command calls
    /// - Handles large repositories efficiently by avoiding full history traversal
    /// </summary>
    public class GitFileLocator : IGitFileLocator
    {
        private readonly IGitHelper _gitHelper;
        private readonly ISearchLogger _logger;
        private readonly IProcessWrapper _processWrapper;

        public GitFileLocator(IGitHelper gitHelper, ISearchLogger logger, IProcessWrapper processWrapper)
        {
            _gitHelper = gitHelper;
            _logger = logger;
            _processWrapper = processWrapper;
        }

        /// <summary>
        /// Locates a file in the Git repository's history and tracks any renames.
        /// Returns the most recent commit hash and file path where the file exists.
        /// </summary>
        /// <param name="fileName">The name of the file to locate (case-insensitive)</param>
        /// <param name="progress">Optional progress reporter for UI updates</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Tuple of (CommitHash, FilePath) where the file was last found, or (null, null) if not found</returns>
        public (string? CommitHash, string? FilePath) LocateFile(string fileName, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            if (!_gitHelper.IsValidRepository())
            {
                _logger.LogError("Not a valid git repository.");
                progress?.Report(1.0); // Report completion even on error
                return (null, null);
            }

            // Try command line approach as it's faster
            var result = LocateFileUsingGitCommand(fileName, progress, cancellationToken);
            if (result.CommitHash != null)
            {
                progress?.Report(1.0); // Ensure we report completion
                return result;
            }

            _logger.WriteLine($"\nFile {fileName} not found.");
            progress?.Report(1.0); // Report completion when file not found
            return (null, null);
        }

        /// <summary>
        /// Uses git commands to efficiently search for a file through the repository's history.
        /// First gets all commit hashes, then uses binary search to locate the file.
        /// </summary>
        private (string? CommitHash, string? FilePath) LocateFileUsingGitCommand(string fileName, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check for cancellation before starting
                cancellationToken.ThrowIfCancellationRequested();
                
                // Get all commit hashes first
                var commits = new List<string>();
                _processWrapper.StartAndProcessOutput(
                    "log --all --pretty=format:%H",
                    _gitHelper.GetRepositoryPath(),
                    line =>
                    {
                        if (line.Length == 40 && line.All(c => char.IsLetterOrDigit(c)))
                        {
                            commits.Add(line);
                        }
                    },
                    cancellationToken
                );

                // Check for cancellation after getting commits
                cancellationToken.ThrowIfCancellationRequested();

                if (commits.Count == 0)
                {
                    _logger.WriteLine("No commits found in repository.");
                    _logger.LogFooter();
                    return (null, null);
                }

                progress?.Report(0.1); // 10% progress after getting commits

                // Use binary search to find the file
                var result = BinarySearchFile(commits, fileName, progress, cancellationToken);
                progress?.Report(1.0); // Ensure we report completion
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.WriteLine("File location operation was cancelled.");
                throw; // Re-throw to signal cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError("Command failed", ex);
                return (null, null);
            }
        }

        /// <summary>
        /// Implements a binary search algorithm to find any occurrence of the file in the commit history.
        /// Once found, tracks the file forward through history to find renames and get the most recent path.
        /// 
        /// The binary search is not a simple binary search because:
        /// 1. The file might exist in multiple discontinuous ranges of commits
        /// 2. We need to search both older and newer commits when a commit doesn't contain the file
        /// 3. We want to find the most recent version of the file after tracking renames
        /// 
        /// The method maintains a path history to show how the file has moved through the repository,
        /// making it easier to understand file reorganizations and refactorings.
        /// </summary>
        private (string? CommitHash, string? FilePath) BinarySearchFile(List<string> commits, string fileName, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            var searchQueue = new Queue<(int start, int end)>();
            searchQueue.Enqueue((0, commits.Count - 1));
            var searchedCommits = new HashSet<string>();
            int totalSearches = 0;

            string? initialCommitHash = null;
            string? initialFilePath = null;
            string? lastFoundPath = null;

            // First use binary search to find any occurrence of the file
            while (searchQueue.Count > 0)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                var (start, end) = searchQueue.Dequeue();
                if (start > end) continue;

                int mid = start + (end - start) / 2;
                var commitHash = commits[mid];

                // Skip if we've already searched this commit
                if (searchedCommits.Contains(commitHash)) continue;
                searchedCommits.Add(commitHash);

                totalSearches++;
                // Report progress between 10% and 95%
                progress?.Report(0.1 + (0.85 * Math.Min(totalSearches, commits.Count) / commits.Count));

                try
                {
                    string? foundPath = null;
                    _processWrapper.StartAndProcessOutput(
                        $"ls-tree --name-only -r {commitHash}",
                        _gitHelper.GetRepositoryPath(),
                        line =>
                        {
                            if (line.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                            {
                                foundPath = line;
                            }
                        },
                        cancellationToken
                    );

                    var commitTime = _gitHelper.GetCommitTime(commitHash);
                    if (foundPath != null && foundPath != lastFoundPath)
                    {
                        _logger.WriteLine($"Checked commit: {commitHash} at {commitTime}, found: {(foundPath != null).ToString().ToLower()}");
                        _logger.WriteLine($"  Path: {foundPath}");
                        lastFoundPath = foundPath;
                    }
                    else
                    {
                        _logger.WriteLine($"Checked commit: {commitHash} at {commitTime}, found: {(foundPath != null).ToString().ToLower()}");
                    }
                    _logger.Flush();

                    if (foundPath != null)
                    {
                        initialCommitHash = commitHash;
                        initialFilePath = foundPath;
                        break; // Found the file, now we'll track its history forward
                    }

                    // If not found, we need to search both ranges as the file might have been added and removed multiple times
                    if (start < mid)
                    {
                        searchQueue.Enqueue((start, mid - 1));
                    }
                    if (mid < end)
                    {
                        searchQueue.Enqueue((mid + 1, end));
                    }
                }
                catch (OperationCanceledException)
                {
                    throw; // Re-throw cancellation
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error checking commit {commitHash}", ex);
                }
            }

            if (initialCommitHash == null || initialFilePath == null)
            {
                _logger.LogFooter();
                _logger.WriteLine($"Processed {totalSearches} commits without finding the file.");
                _logger.WriteLine($"\nFile {fileName} not found.");
                return (null, null);
            }

            // Check for cancellation before tracking history
            cancellationToken.ThrowIfCancellationRequested();

            // Now track the file forward from the initial commit to find renames
            _logger.WriteLine($"\nTracking file history forward from commit {initialCommitHash}...");
            string currentCommitHash = initialCommitHash;
            string currentFilePath = initialFilePath;

            // Store path history in chronological order (oldest to newest)
            var pathHistory = new List<(string CommitHash, string Path, string Time)>();
            pathHistory.Add((initialCommitHash, initialFilePath, _gitHelper.GetCommitTime(initialCommitHash)));

            try
            {
                string? lastValidCommit = null;
                string? lastValidPath = null;

                _processWrapper.StartAndProcessOutput(
                    $"log --follow --name-status {initialCommitHash}..HEAD",
                    _gitHelper.GetRepositoryPath(),
                    line =>
                    {
                        if (line.Length == 40 && line.All(c => char.IsLetterOrDigit(c)))
                        {
                            currentCommitHash = line;
                        }
                        else if (line.StartsWith("R"))
                        {
                            var parts = line.Split('\t');
                            if (parts.Length == 3)
                            {
                                var oldPath = parts[1];
                                var newPath = parts[2];
                                if (oldPath == currentFilePath)
                                {
                                    var commitTime = _gitHelper.GetCommitTime(currentCommitHash);
                                    _logger.WriteLine($"File renamed in commit {currentCommitHash} at {commitTime}:");
                                    _logger.WriteLine($"  From: {oldPath}");
                                    _logger.WriteLine($"  To:   {newPath}");
                                    currentFilePath = newPath;
                                    lastValidCommit = currentCommitHash;
                                    lastValidPath = newPath;
                                    pathHistory.Add((currentCommitHash, newPath, commitTime));
                                }
                            }
                        }
                        else if (line.StartsWith("D") && line.EndsWith(currentFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            var commitTime = _gitHelper.GetCommitTime(currentCommitHash);
                            _logger.WriteLine($"File was deleted in commit {currentCommitHash} at {commitTime}");
                        }
                    },
                    cancellationToken
                );

                // If we found a more recent version of the file, return that
                if (lastValidCommit != null && lastValidPath != null)
                {
                    _logger.WriteLine("\nFile path history (from oldest to newest):");
                    foreach (var (commitHash, path, time) in pathHistory)
                    {
                        _logger.WriteLine($"Commit {commitHash} ({time}):");
                        _logger.WriteLine($"  Path: {path}");
                    }
                    
                    return (lastValidCommit, lastValidPath);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error tracking file history: {ex.Message}");
            }

            // If we couldn't track forward or there were no renames, show the only path we found
            _logger.WriteLine("\nFile path history:");
            foreach (var (commitHash, path, time) in pathHistory)
            {
                _logger.WriteLine($"Commit {commitHash} ({time}):");
                _logger.WriteLine($"  Path: {path}");
            }
            
            return (initialCommitHash, initialFilePath);
        }
    }
} 