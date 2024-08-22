using System;
using System.IO;
using Xunit;
using Moq;

namespace GitContentSearch.Tests
{
    public class GitContentSearcherTests
    {
        [Fact]
        public void SearchContent_ShouldLogCorrectly_ForFoundString()
        {
            // Arrange
            var gitHelperMock = new Mock<GitHelper>();
            var fileSearcherMock = new Mock<FileSearcher>();

            gitHelperMock.Setup(g => g.GetGitCommits(It.IsAny<string>(), It.IsAny<string>()))
                         .Returns(new[] { "commit1", "commit2" });
            gitHelperMock.Setup(g => g.GetCommitTime(It.IsAny<string>()))
                         .Returns("2023-08-21");

            fileSearcherMock.Setup(f => f.SearchInFile(It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(true);

            var gitContentSearcher = new GitContentSearcher(gitHelperMock.Object, fileSearcherMock.Object);

            using (var logFile = new StreamWriter("search_log.txt", append: false))
            {
                // Act
                gitContentSearcher.SearchContent("dummy/path.txt", "search string");

                // Assert
                logFile.Flush();
                logFile.BaseStream.Position = 0;
                var logContent = new StreamReader(logFile.BaseStream).ReadToEnd();
                Assert.Contains("found: True", logContent);
            }
        }

        [Fact]
        public void SearchContent_ShouldLogCorrectly_ForNotFoundString()
        {
            // Arrange
            var gitHelperMock = new Mock<GitHelper>();
            var fileSearcherMock = new Mock<FileSearcher>();

            gitHelperMock.Setup(g => g.GetGitCommits(It.IsAny<string>(), It.IsAny<string>()))
                         .Returns(new[] { "commit1", "commit2" });
            gitHelperMock.Setup(g => g.GetCommitTime(It.IsAny<string>()))
                         .Returns("2023-08-21");

            fileSearcherMock.Setup(f => f.SearchInFile(It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(false);

            var gitContentSearcher = new GitContentSearcher(gitHelperMock.Object, fileSearcherMock.Object);

            using (var logFile = new StreamWriter("search_log.txt", append: false))
            {
                // Act
                gitContentSearcher.SearchContent("dummy/path.txt", "search string");

                // Assert
                logFile.Flush();
                logFile.BaseStream.Position = 0;
                var logContent = new StreamReader(logFile.BaseStream).ReadToEnd();
                Assert.Contains("found: False", logContent);
            }
        }
    }
}
