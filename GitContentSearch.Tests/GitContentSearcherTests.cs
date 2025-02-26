using GitContentSearch.Helpers;
using System;
using System.IO;
using Xunit;

namespace GitContentSearch.Tests
{
	public class GitContentSearcherTests : IDisposable
	{
		private readonly TestRepositoryHelper _repoHelper;
		private readonly string _testFilePath = "test.txt";

		public GitContentSearcherTests()
		{
			_repoHelper = new TestRepositoryHelper();
		}

		public void Dispose()
		{
			_repoHelper.Dispose();
		}

		[Fact]
		public void SearchContent_ShouldLogFirstAndLastAppearance_Correctly()
		{
			// Arrange
			_repoHelper.CreateAndCommitFile(_testFilePath, "Initial content", "Initial commit"); // commit1
			var commit1 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Content with search string", "Add search string"); // commit2
			var commit2 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Content with search string again", "Keep search string"); // commit3
			var commit3 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Content with search string still", "Still has search string"); // commit4
			var commit4 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Final content", "Remove search string"); // commit5
			var commit5 = _repoHelper.GetLastCommitHash();

			var gitHelper = new GitHelper(new ProcessWrapper(), _repoHelper.RepositoryPath);
			var fileSearcher = new FileSearcher();
			var fileManager = new FileManager();

			using var stringWriter = new StringWriter();
			var gitContentSearcher = new GitContentSearcher(gitHelper, fileSearcher, fileManager, disableLinearSearch: false, logWriter: stringWriter);

			// Act
			gitContentSearcher.SearchContent(_testFilePath, "search string");

			// Assert
			var logContent = stringWriter.ToString();
			Assert.Contains($"Search string \"search string\" first appears in commit {commit2}", logContent);
			Assert.Contains($"Search string \"search string\" last appears in commit {commit4}", logContent);
		}

		[Fact]
		public void SearchContent_ShouldLogCorrectly_WhenStringNotFound()
		{
			// Arrange
			_repoHelper.CreateAndCommitFile(_testFilePath, "Initial content", "Initial commit"); // commit1
			_repoHelper.CreateAndCommitFile(_testFilePath, "Different content", "Second commit"); // commit2
			_repoHelper.CreateAndCommitFile(_testFilePath, "More content", "Third commit"); // commit3
			_repoHelper.CreateAndCommitFile(_testFilePath, "Other content", "Fourth commit"); // commit4
			_repoHelper.CreateAndCommitFile(_testFilePath, "Final content", "Final commit"); // commit5

			var gitHelper = new GitHelper(new ProcessWrapper(), _repoHelper.RepositoryPath);
			var fileSearcher = new FileSearcher();
			var fileManager = new FileManager();

			using var stringWriter = new StringWriter();
			var gitContentSearcher = new GitContentSearcher(gitHelper, fileSearcher, fileManager, disableLinearSearch: true, logWriter: stringWriter);

			// Act
			gitContentSearcher.SearchContent(_testFilePath, "search string");

			// Assert
			var logContent = stringWriter.ToString();
			Assert.Contains("Search string \"search string\" does not appear in any of the checked commits.", logContent);
		}

		[Fact]
		public void SearchContent_ShouldLogCorrectly_WhenStringAppearsInSingleCommit()
		{
			// Arrange
			_repoHelper.CreateAndCommitFile(_testFilePath, "Initial content", "Initial commit"); // commit1
			var commit1 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Different content", "Second commit"); // commit2
			var commit2 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Content with search string", "Add search string"); // commit3
			var commit3 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Other content", "Remove search string"); // commit4
			var commit4 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Final content", "Final commit"); // commit5
			var commit5 = _repoHelper.GetLastCommitHash();

			var gitHelper = new GitHelper(new ProcessWrapper(), _repoHelper.RepositoryPath);
			var fileSearcher = new FileSearcher();
			var fileManager = new FileManager();

			using var stringWriter = new StringWriter();
			var gitContentSearcher = new GitContentSearcher(gitHelper, fileSearcher, fileManager, disableLinearSearch: true, logWriter: stringWriter);

			// Act
			gitContentSearcher.SearchContent(_testFilePath, "search string");

			// Assert
			var logContent = stringWriter.ToString();
			Assert.Contains($"Search string \"search string\" first appears in commit {commit3}", logContent);
			Assert.Contains($"Search string \"search string\" last appears in commit {commit3}", logContent);
		}

		[Fact]
		public void SearchContent_ShouldRestrictSearchToSpecifiedCommitRange()
		{
			// Arrange
			_repoHelper.CreateAndCommitFile(_testFilePath, "Initial content", "Initial commit"); // commit1
			var commit1 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Content with search string", "Add search string"); // commit2
			var commit2 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Different content", "Remove search string"); // commit3
			var commit3 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "More content", "Another change"); // commit4
			var commit4 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Final content", "Final change"); // commit5
			var commit5 = _repoHelper.GetLastCommitHash();

			var gitHelper = new GitHelper(new ProcessWrapper(), _repoHelper.RepositoryPath);
			var fileSearcher = new FileSearcher();
			var fileManager = new FileManager();

			using var stringWriter = new StringWriter();
			var gitContentSearcher = new GitContentSearcher(gitHelper, fileSearcher, fileManager, disableLinearSearch: true, logWriter: stringWriter);

			// Act - Pass commit2 as earliest (older) and commit3 as latest (newer)
			gitContentSearcher.SearchContent(_testFilePath, "search string", commit2, commit3);

			// Assert
			var logContent = stringWriter.ToString();

			// Ensure only the specified commit range was searched
			Assert.Contains($"Checked commit: {commit3}", logContent);
			Assert.Contains($"Checked commit: {commit2}", logContent);
			Assert.DoesNotContain($"Checked commit: {commit1}", logContent);
			Assert.DoesNotContain($"Checked commit: {commit4}", logContent);
			Assert.DoesNotContain($"Checked commit: {commit5}", logContent);

			// Check the log for the correct first and last appearance commits within the range
			Assert.Contains($"Search string \"search string\" first appears in commit {commit2}", logContent);
			Assert.Contains($"Search string \"search string\" last appears in commit {commit2}", logContent);
		}

		[Fact]
		public void SearchContent_ShouldLogError_WhenEarliestCommitIsMoreRecentThanLatestCommit()
		{
			// Arrange
			_repoHelper.CreateAndCommitFile(_testFilePath, "Initial content", "Initial commit"); // commit1
			var commit1 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Content with search string", "Add search string"); // commit2
			var commit2 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Different content", "Remove search string"); // commit3
			var commit3 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "More content", "Another change"); // commit4
			var commit4 = _repoHelper.GetLastCommitHash();

			var gitHelper = new GitHelper(new ProcessWrapper(), _repoHelper.RepositoryPath);
			var fileSearcher = new FileSearcher();
			var fileManager = new FileManager();

			using var stringWriter = new StringWriter();
			var gitContentSearcher = new GitContentSearcher(gitHelper, fileSearcher, fileManager, disableLinearSearch: true, logWriter: stringWriter);

			// Act - Pass commit3 as earliest (more recent) and commit2 as latest (older)
			gitContentSearcher.SearchContent(_testFilePath, "search string", commit3, commit2);

			// Assert
			var logContent = stringWriter.ToString();
			Assert.Contains("Error: The earliest commit is more recent than the latest commit.", logContent);
		}

		[Fact]
		public void SearchContent_ShouldFindMatch_WhenBinarySearchMisses_LinearSearchEnabled()
		{
			// Arrange
			_repoHelper.CreateAndCommitFile(_testFilePath, "Initial content", "Initial commit"); // commit1
			var commit1 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Content with search string", "Add search string"); // commit2
			var commit2 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Different content", "Remove search string"); // commit3
			var commit3 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "More content", "Another change"); // commit4
			var commit4 = _repoHelper.GetLastCommitHash();

			_repoHelper.CreateAndCommitFile(_testFilePath, "Final content", "Final change"); // commit5
			var commit5 = _repoHelper.GetLastCommitHash();

			var gitHelper = new GitHelper(new ProcessWrapper(), _repoHelper.RepositoryPath);
			var fileSearcher = new FileSearcher();
			var fileManager = new FileManager();

			using var stringWriter = new StringWriter();
			var gitContentSearcher = new GitContentSearcher(gitHelper, fileSearcher, fileManager, disableLinearSearch: false, logWriter: stringWriter);

			// Act
			gitContentSearcher.SearchContent(_testFilePath, "search string");

			// Assert
			var logContent = stringWriter.ToString();
			Assert.Contains($"Search string \"search string\" first appears in commit {commit2}", logContent);
			Assert.Contains($"Search string \"search string\" last appears in commit {commit2}", logContent);
		}
	}
}