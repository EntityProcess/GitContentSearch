using Moq;
using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using LibGit2Sharp;

namespace GitContentSearch.Tests
{
	public class GitHelperTests : IDisposable
	{
		private readonly TestRepositoryHelper _repoHelper;

		public GitHelperTests()
		{
			_repoHelper = new TestRepositoryHelper();
		}

		public void Dispose()
		{
			_repoHelper.Dispose();
		}

		[Fact]
		public void GetGitCommits_ShouldReturnEmptyList_OnGitFailure()
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();

			var processResult = new ProcessResult(string.Empty, "Error occurred", 1);
			processWrapperMock.Setup(pw => pw.Start(It.IsAny<ProcessStartInfo>(), null)).Returns(processResult);

			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Act
			var result = gitHelper.GetGitCommits("invalidCommit", "anotherInvalidCommit");

			// Assert
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public void GetCommitTime_ShouldThrowException_OnProcessFailure()
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();
			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Act & Assert
			var exception = Assert.Throws<ArgumentException>(() => gitHelper.GetCommitTime("invalidCommit"));
			Assert.Equal("Invalid commit hash: invalidCommit", exception.Message);
		}

		[Fact]
		public void GetCommitTime_ShouldReturnCorrectTime_OnSuccess()
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();
			_repoHelper.CreateAndCommitFile("test.txt", "test content", "test commit");
			var commitHash = _repoHelper.GetLastCommitHash();
			var gitHelper = new GitHelper(processWrapperMock.Object, _repoHelper.RepositoryPath);

			// Act
			var result = gitHelper.GetCommitTime(commitHash);

			// Assert
			// Since the commit time is generated at runtime, we just verify the format
			Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} [+-]\d{2}:\d{2}", result);
		}

		[Fact]
		public void GetGitCommits_ShouldReturnCorrectRange_WhenBothCommitsArePresent()
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();

			var gitLogOutput = "commit5\ncommit4\ncommit3\ncommit2\ncommit1";
			var processResult = new ProcessResult(gitLogOutput, string.Empty, 0);
			processWrapperMock.Setup(pw => pw.Start(It.Is<string>(x => x.Trim() == "log --pretty=format:%H"), null, null)).Returns(processResult);

			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Act
			var result = gitHelper.GetGitCommits("commit2", "commit4");

			// Assert
			var expectedCommits = new List<Commit>
							{
								new Commit("commit4", ""),
								new Commit("commit3", ""),
								new Commit("commit2", "")
							};

			AssertCommitsAreEqual(expectedCommits, result);
		}

		[Fact]
		public void GetGitCommits_ShouldReturnFullRange_WhenEarliestAndLatestAreEmpty()
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();

			var gitLogOutput = "commit5\ncommit4\ncommit3\ncommit2\ncommit1";
			var processResult = new ProcessResult(gitLogOutput, string.Empty, 0);
			processWrapperMock.Setup(pw => pw.Start(It.Is<string>(x => x.Trim() == "log --pretty=format:%H"), null, null)).Returns(processResult);

			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Act
			var result = gitHelper.GetGitCommits("", "");

			// Assert
			var expectedCommits = new List<Commit>
							{
								new Commit("commit5", ""),
								new Commit("commit4", ""),
								new Commit("commit3", ""),
								new Commit("commit2", ""),
								new Commit("commit1", "")
							};

			AssertCommitsAreEqual(expectedCommits, result);
		}

		[Fact]
		public void GetGitCommits_ShouldReturnPartialRange_WhenOnlyEarliestCommitIsSpecified()
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();

			var gitLogOutput = "commit5\ncommit4\ncommit3\ncommit2\ncommit1";
			var processResult = new ProcessResult(gitLogOutput, string.Empty, 0);
			processWrapperMock.Setup(pw => pw.Start(It.Is<string>(x => x.Trim() == "log --pretty=format:%H"), null, null)).Returns(processResult);

			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Act
			var result = gitHelper.GetGitCommits("commit3", "");

			// Assert
			var expectedCommits = new List<Commit>
							{
								new Commit("commit5", ""),
								new Commit("commit4", ""),
								new Commit("commit3", "")
							};

			AssertCommitsAreEqual(expectedCommits, result);
		}

		[Fact]
		public void GetGitCommits_ShouldReturnPartialRange_WhenOnlyLatestCommitIsSpecified()
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();

			var gitLogOutput = "commit5\ncommit4\ncommit3\ncommit2\ncommit1";
			var processResult = new ProcessResult(gitLogOutput, string.Empty, 0);
			processWrapperMock.Setup(pw => pw.Start(It.Is<string>(x => x.Trim() == "log --pretty=format:%H"), null, null)).Returns(processResult);

			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Act
			var result = gitHelper.GetGitCommits("", "commit3");

			// Assert
			var expectedCommits = new List<Commit>
							{
								new Commit("commit3", ""),
								new Commit("commit2", ""),
								new Commit("commit1", "")
							};

			AssertCommitsAreEqual(expectedCommits, result);
		}

		[Fact]
		public void GetGitCommits_ShouldReturnEmptyList_WhenCommitsAreInWrongOrder()
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();

			var gitLogOutput = "commit5\ncommit4\ncommit3\ncommit2\ncommit1";
			var processResult = new ProcessResult(gitLogOutput, string.Empty, 0);
			processWrapperMock.Setup(pw => pw.Start(It.IsAny<ProcessStartInfo>(), null)).Returns(processResult);

			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Act
			var result = gitHelper.GetGitCommits("commit4", "commit2");

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void GetGitCommits_ShouldReturnEmptyList_WhenEarliestCommitIsNotFound()
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();

			var gitLogOutput = "commit5\ncommit4\ncommit3\ncommit2\ncommit1";
			var processResult = new ProcessResult(gitLogOutput, string.Empty, 0);
			processWrapperMock.Setup(pw => pw.Start(It.IsAny<ProcessStartInfo>(), null)).Returns(processResult);

			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Act
			var result = gitHelper.GetGitCommits("nonexistentCommit", "commit2");

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void GetGitCommits_ShouldReturnEmptyList_WhenLatestCommitIsNotFound()
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();

			var gitLogOutput = "commit5\ncommit4\ncommit3\ncommit2\ncommit1";
			var processResult = new ProcessResult(gitLogOutput, string.Empty, 0);
			processWrapperMock.Setup(pw => pw.Start(It.IsAny<ProcessStartInfo>(), null)).Returns(processResult);

			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Act
			var result = gitHelper.GetGitCommits("commit4", "nonexistentCommit");

			// Assert
			Assert.Empty(result);
		}

		private void AssertCommitsAreEqual(List<Commit> expectedCommits, List<Commit> actualCommits)
		{
			Assert.Equal(expectedCommits.Count, actualCommits.Count);
			for (int i = 0; i < expectedCommits.Count; i++)
			{
				Assert.Equal(expectedCommits[i].CommitHash, actualCommits[i].CommitHash);
			}
		}

		[Theory]
		[InlineData("/path/to/file.txt", "log --pretty=format:%H -- \"path/to/file.txt\"")]
		[InlineData("path/to/file.txt", "log --pretty=format:%H -- \"path/to/file.txt\"")]
		[InlineData("file with spaces.txt", "log --pretty=format:%H -- \"file with spaces.txt\"")]
		public void GetCommits_ShouldFormatFilePath_Correctly(string inputPath, string expectedCommand)
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();
			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Setup mock to capture the actual command
			string? capturedCommand = null;
			processWrapperMock
				.Setup(pw => pw.Start(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>()))
				.Callback<string, string?, Stream?>((cmd, dir, stream) => capturedCommand = cmd)
				.Returns(new ProcessResult(string.Empty, string.Empty, 0));

			// Act
			gitHelper.GetGitCommits(string.Empty, string.Empty, inputPath);

			// Assert
			Assert.Equal(expectedCommand.Trim(), capturedCommand?.Trim());
		}

		[Theory]
		[InlineData("/path/to/file.txt", "log --name-status --pretty=format:%H --follow -- \"path/to/file.txt\"")]
		[InlineData("path/to/file.txt", "log --name-status --pretty=format:%H --follow -- \"path/to/file.txt\"")]
		[InlineData("file with spaces.txt", "log --name-status --pretty=format:%H --follow -- \"file with spaces.txt\"")]
		public void GetCommitsWithFollow_ShouldFormatFilePath_Correctly(string inputPath, string expectedCommand)
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();
			var gitHelper = new GitHelper(processWrapperMock.Object, null, true);  // true for follow flag

			// Setup mock to capture the actual command
			string? capturedCommand = null;
			processWrapperMock
				.Setup(pw => pw.Start(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>()))
				.Callback<string, string?, Stream?>((cmd, dir, stream) => capturedCommand = cmd)
				.Returns(new ProcessResult(string.Empty, string.Empty, 0));

			// Act
			gitHelper.GetGitCommits(string.Empty, string.Empty, inputPath);

			// Assert
			Assert.Equal(expectedCommand.Trim(), capturedCommand?.Trim());
		}
	}
}