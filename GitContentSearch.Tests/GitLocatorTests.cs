using Moq;
using Xunit;

namespace GitContentSearch.Tests
{
    public class GitLocatorTests
    {
        private readonly Mock<IGitHelper> _mockGitHelper;
        private readonly Mock<TextWriter> _mockLogWriter;
        private readonly GitLocator _gitLocator;

        public GitLocatorTests()
        {
            _mockGitHelper = new Mock<IGitHelper>();
            _mockLogWriter = new Mock<TextWriter>();
            _gitLocator = new GitLocator(_mockGitHelper.Object, _mockLogWriter.Object);
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
            _mockLogWriter.Verify(x => x.WriteLine("Error: Not a valid git repository."), Times.Once);
        }

        [Fact]
        public void LocateFile_FileNotFound_ReturnsNull()
        {
            // Arrange
            _mockGitHelper.Setup(x => x.IsValidRepository()).Returns(true);
            _mockGitHelper.Setup(x => x.GetGitCommits(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                         .Returns(new List<Commit>());

            // Act
            var result = _gitLocator.LocateFile("nonexistent.txt");

            // Assert
            Assert.Null(result.CommitHash);
            Assert.Null(result.FilePath);
            _mockLogWriter.Verify(x => x.WriteLine("File 'nonexistent.txt' not found in any commit."), Times.Once);
        }

        [Fact]
        public void LocateFile_FileFound_ReturnsLatestCommit()
        {
            // Arrange
            const string expectedCommitHash = "abc123";
            const string expectedFilePath = "src/test.txt";
            const string expectedCommitTime = "2024-03-20 10:00:00 +00:00";

            _mockGitHelper.Setup(x => x.IsValidRepository()).Returns(true);
            _mockGitHelper.Setup(x => x.GetGitCommits(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                         .Returns(new List<Commit> { new Commit(expectedCommitHash, expectedFilePath) });
            _mockGitHelper.Setup(x => x.GetCommitTime(expectedCommitHash))
                         .Returns(expectedCommitTime);

            // Act
            var result = _gitLocator.LocateFile("test.txt");

            // Assert
            Assert.Equal(expectedCommitHash, result.CommitHash);
            Assert.Equal(expectedFilePath, result.FilePath);
            _mockLogWriter.Verify(x => x.WriteLine($"Found 'test.txt' in commit {expectedCommitHash} ({expectedCommitTime})"), Times.Once);
            _mockLogWriter.Verify(x => x.WriteLine($"Full path: {expectedFilePath}"), Times.Once);
        }
    }
} 