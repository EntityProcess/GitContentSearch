using Moq;
using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace GitContentSearch.Tests
{
	public class GitHelperTests
	{
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

			var processResult = new ProcessResult(string.Empty, "fatal: bad object invalidCommit", 1);
			processWrapperMock.Setup(pw => pw.Start(It.IsAny<string>(), null, null)).Returns(processResult);

			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Act & Assert
			var exception = Assert.Throws<Exception>(() => gitHelper.GetCommitTime("invalidCommit"));
			Assert.Equal("Error getting commit time: fatal: bad object invalidCommit", exception.Message);
		}

		[Fact]
		public void GetCommitTime_ShouldReturnCorrectTime_OnSuccess()
		{
			// Arrange
			var processWrapperMock = new Mock<IProcessWrapper>();

			var processResult = new ProcessResult("2023-08-21 12:34:56 +0000", string.Empty, 0);
			processWrapperMock.Setup(pw => pw.Start(It.IsAny<string>(), null, null)).Returns(processResult);

			var gitHelper = new GitHelper(processWrapperMock.Object);

			// Act
			var result = gitHelper.GetCommitTime("validCommitHash");

			// Assert
			Assert.Equal("2023-08-21 12:34:56 +0000", result);
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
	}
}