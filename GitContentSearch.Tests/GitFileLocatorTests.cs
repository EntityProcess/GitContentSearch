using GitContentSearch.Interfaces;
using Moq;
using System.Threading;

namespace GitContentSearch.Tests
{
	public class GitFileLocatorTests
    {
        private readonly Mock<IGitHelper> _mockGitHelper;
        private readonly Mock<ISearchLogger> _mockLogWriter;
        private readonly Mock<IProcessWrapper> _mockProcessWrapper;
        private readonly GitFileLocator _gitLocator;

        public GitFileLocatorTests()
        {
            _mockGitHelper = new Mock<IGitHelper>();
            _mockLogWriter = new Mock<ISearchLogger>();
            _mockProcessWrapper = new Mock<IProcessWrapper>();
            _gitLocator = new GitFileLocator(_mockGitHelper.Object, _mockLogWriter.Object, _mockProcessWrapper.Object);
        }

        [Fact]
        public void LocateFile_InvalidRepository_ReturnsNull()
        {
            // Arrange
            _mockGitHelper.Setup(x => x.IsValidRepository()).Returns(false);

            // Act
            var result = _gitLocator.LocateFile("test.txt");

            // Assert
            Assert.Null(result.CommitHash);
            Assert.Null(result.FilePath);
            _mockLogWriter.Verify(x => x.LogError("Not a valid git repository.", null), Times.Once);
        }

        [Fact]
        public void LocateFile_FileNotFound_ReturnsNull()
        {
            // Arrange
            _mockGitHelper.Setup(x => x.IsValidRepository()).Returns(true);
            _mockGitHelper.Setup(x => x.GetRepositoryPath()).Returns("dummy/path");
            _mockProcessWrapper.Setup(x => x.StartAndProcessOutput(
                It.Is<string>(cmd => cmd == "log --all --pretty=format:%H"),
                It.IsAny<string>(),
                It.IsAny<Action<string>>(),
                It.IsAny<CancellationToken>()
            ));

            // Act
            var result = _gitLocator.LocateFile("nonexistent.txt");

            // Assert
            Assert.Null(result.CommitHash);
            Assert.Null(result.FilePath);
            _mockLogWriter.Verify(x => x.WriteLine("No commits found in repository."), Times.Once);
            _mockLogWriter.Verify(x => x.LogFooter(), Times.Once);
            _mockLogWriter.Verify(x => x.WriteLine("\nFile nonexistent.txt not found."), Times.Once);
        }

        [Fact]
        public void LocateFile_FileFound_ReturnsLatestCommit()
        {
            // Arrange
            const string expectedCommitHash = "abc123";
            const string expectedFilePath = "src/test.txt";
            const string searchFileName = "test.txt";
            const string expectedCommitTime = "2024-03-20 10:00:00 +00:00";

            _mockGitHelper.Setup(x => x.IsValidRepository()).Returns(true);
            _mockGitHelper.Setup(x => x.GetRepositoryPath()).Returns("dummy/path");

            // Make the commit hash 40 characters to match git's format
            var fullCommitHash = expectedCommitHash.PadRight(40, '0');
            
            _mockGitHelper.Setup(x => x.GetCommitTime(fullCommitHash))
                         .Returns(expectedCommitTime);

            bool processOutputCalled = false;

            _mockProcessWrapper
                .Setup(x => x.StartAndProcessOutput(
                    It.Is<string>(cmd => cmd == "log --all --pretty=format:%H"),
                    It.IsAny<string>(),
                    It.IsAny<Action<string>>(),
                    It.IsAny<CancellationToken>()))
                .Callback((string cmd, string dir, Action<string> callback, CancellationToken token) =>
                {
                    callback(fullCommitHash);
                    processOutputCalled = true;
                });

            _mockProcessWrapper
                .Setup(x => x.StartAndProcessOutput(
                    It.Is<string>(cmd => cmd == $"ls-tree --name-only -r {fullCommitHash}"),
                    It.IsAny<string>(),
                    It.IsAny<Action<string>>(),
                    It.IsAny<CancellationToken>()))
                .Callback((string cmd, string dir, Action<string> callback, CancellationToken token) =>
                {
                    callback(expectedFilePath);
                });

            _mockProcessWrapper
                .Setup(x => x.StartAndProcessOutput(
                    It.Is<string>(cmd => cmd == $"log --follow --name-status {fullCommitHash}..HEAD"),
                    It.IsAny<string>(),
                    It.IsAny<Action<string>>(),
                    It.IsAny<CancellationToken>()));

            // Act
            var result = _gitLocator.LocateFile(searchFileName);

            // Assert
            Assert.True(processOutputCalled, "Process output callback was not called");
            Assert.Equal(fullCommitHash, result.CommitHash);
            Assert.Equal(expectedFilePath, result.FilePath);
            _mockLogWriter.Verify(x => x.WriteLine($"Checked commit: {fullCommitHash} at {expectedCommitTime}, found: true"), Times.Once);
            _mockLogWriter.Verify(x => x.WriteLine($"  Path: {expectedFilePath}"), Times.AtLeastOnce);
        }
    }
} 