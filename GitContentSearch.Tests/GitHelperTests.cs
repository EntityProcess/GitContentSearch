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
        public void GetGitCommits_ShouldReturnEmptyArray_OnGitFailure()
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
            processWrapperMock.Setup(pw => pw.Start(It.IsAny<ProcessStartInfo>(), null)).Returns(processResult);

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
            processWrapperMock.Setup(pw => pw.Start(It.IsAny<ProcessStartInfo>(), null)).Returns(processResult);

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
            processWrapperMock.Setup(pw => pw.Start(It.IsAny<ProcessStartInfo>(), null)).Returns(processResult);

            var gitHelper = new GitHelper(processWrapperMock.Object);

            // Act
            var result = gitHelper.GetGitCommits("commit2", "commit4");

            // Assert
            Assert.Equal(new[] { "commit4", "commit3", "commit2" }, result);
        }

        [Fact]
        public void GetGitCommits_ShouldReturnFullRange_WhenEarliestAndLatestAreEmpty()
        {
            // Arrange
            var processWrapperMock = new Mock<IProcessWrapper>();

            var gitLogOutput = "commit5\ncommit4\ncommit3\ncommit2\ncommit1";
            var processResult = new ProcessResult(gitLogOutput, string.Empty, 0);
            processWrapperMock.Setup(pw => pw.Start(It.IsAny<ProcessStartInfo>(), null)).Returns(processResult);

            var gitHelper = new GitHelper(processWrapperMock.Object);

            // Act
            var result = gitHelper.GetGitCommits("", "");

            // Assert
            Assert.Equal(new[] { "commit5", "commit4", "commit3", "commit2", "commit1" }, result);
        }

        [Fact]
        public void GetGitCommits_ShouldReturnPartialRange_WhenOnlyEarliestCommitIsSpecified()
        {
            // Arrange
            var processWrapperMock = new Mock<IProcessWrapper>();

            var gitLogOutput = "commit5\ncommit4\ncommit3\ncommit2\ncommit1";
            var processResult = new ProcessResult(gitLogOutput, string.Empty, 0);
            processWrapperMock.Setup(pw => pw.Start(It.IsAny<ProcessStartInfo>(), null)).Returns(processResult);

            var gitHelper = new GitHelper(processWrapperMock.Object);

            // Act
            var result = gitHelper.GetGitCommits("commit3", "");

            // Assert
            Assert.Equal(new[] { "commit5", "commit4", "commit3" }, result);
        }

        [Fact]
        public void GetGitCommits_ShouldReturnPartialRange_WhenOnlyLatestCommitIsSpecified()
        {
            // Arrange
            var processWrapperMock = new Mock<IProcessWrapper>();

            var gitLogOutput = "commit5\ncommit4\ncommit3\ncommit2\ncommit1";
            var processResult = new ProcessResult(gitLogOutput, string.Empty, 0);
            processWrapperMock.Setup(pw => pw.Start(It.IsAny<ProcessStartInfo>(), null)).Returns(processResult);

            var gitHelper = new GitHelper(processWrapperMock.Object);

            // Act
            var result = gitHelper.GetGitCommits("", "commit3");

            // Assert
            Assert.Equal(new[] { "commit3", "commit2", "commit1" }, result);
        }

        [Fact]
        public void GetGitCommits_ShouldReturnEmptyArray_WhenCommitsAreInWrongOrder()
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
        public void GetGitCommits_ShouldReturnEmptyArray_WhenEarliestCommitIsNotFound()
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
        public void GetGitCommits_ShouldReturnEmptyArray_WhenLatestCommitIsNotFound()
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
    }
}