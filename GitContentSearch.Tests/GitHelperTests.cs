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
            var processMock = new Mock<IProcessWrapper>();
            var process = new Mock<Process>();
            processMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>())).Returns(process.Object);

            var outputStream = new MemoryStream();
            var writer = new StreamWriter(outputStream);
            writer.Write(string.Empty);
            writer.Flush();
            outputStream.Position = 0;

            process.Setup(p => p.StandardOutput).Returns(new StreamReader(outputStream));
            process.Setup(p => p.StandardError).Returns(new StreamReader(new MemoryStream()));
            process.Setup(p => p.ExitCode).Returns(1); // Simulate a failure

            var gitHelper = new GitHelper(processMock.Object);

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
            var processMock = new Mock<IProcessWrapper>();
            var process = new Mock<Process>();
            processMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>())).Returns(process.Object);

            var outputStream = new MemoryStream();
            var writer = new StreamWriter(outputStream);
            writer.Write(string.Empty);
            writer.Flush();
            outputStream.Position = 0;

            process.Setup(p => p.StandardOutput).Returns(new StreamReader(outputStream));
            process.Setup(p => p.StandardError).Returns(new StreamReader(new MemoryStream()));
            process.Setup(p => p.ExitCode).Returns(1); // Simulate a failure

            var gitHelper = new GitHelper(processMock.Object);

            // Act & Assert
            Assert.Throws<Exception>(() => gitHelper.GetCommitTime("invalidCommit"));
        }
    }
}
