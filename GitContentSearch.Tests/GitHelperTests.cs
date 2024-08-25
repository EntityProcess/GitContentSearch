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
    }
}