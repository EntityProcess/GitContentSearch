using Moq;
using System;
using System.IO;
using Xunit;

namespace GitContentSearch.Tests
{
    public class GitContentSearcherTests
    {
        [Fact]
        public void SearchContent_ShouldLogFirstAndLastAppearance_Correctly()
        {
            // Arrange
            var gitHelperMock = new Mock<IGitHelper>();
            var fileSearcherMock = new Mock<IFileSearcher>();

            // Simulate commits
            var commits = new[] { "commit1", "commit2", "commit3", "commit4", "commit5" };
            gitHelperMock.Setup(g => g.GetGitCommits(It.IsAny<string>(), It.IsAny<string>())).Returns(commits);
            gitHelperMock.Setup(g => g.GetCommitTime(It.IsAny<string>())).Returns("2023-08-21 12:00:00");

            // Simulate string appearances based on the specific commit
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit1")), It.IsAny<string>())).Returns(false);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit2")), It.IsAny<string>())).Returns(true);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit3")), It.IsAny<string>())).Returns(true);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit4")), It.IsAny<string>())).Returns(true);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit5")), It.IsAny<string>())).Returns(false);

            var gitContentSearcher = new GitContentSearcher(gitHelperMock.Object, fileSearcherMock.Object);

            using (var stringWriter = new StringWriter())
            {
                // Act
                gitContentSearcher.SearchContent("dummy/path.txt", "search string", logWriter: stringWriter);

                // Assert
                var logContent = stringWriter.ToString();

                // Check the log for the correct first and last appearance commits
                Assert.Contains("Search string \"search string\" first appears in commit commit2.", logContent);
                Assert.Contains("Search string \"search string\" last appears in commit commit4.", logContent);
            }
        }

        [Fact]
        public void SearchContent_ShouldLogCorrectly_WhenStringNotFound()
        {
            // Arrange
            var gitHelperMock = new Mock<IGitHelper>();
            var fileSearcherMock = new Mock<IFileSearcher>();

            // Simulate commits
            var commits = new[] { "commit1", "commit2", "commit3", "commit4", "commit5" };
            gitHelperMock.Setup(g => g.GetGitCommits(It.IsAny<string>(), It.IsAny<string>())).Returns(commits);
            gitHelperMock.Setup(g => g.GetCommitTime(It.IsAny<string>())).Returns("2023-08-21 12:00:00");

            // Simulate no appearances of the string
            fileSearcherMock.Setup(f => f.SearchInFile(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            var gitContentSearcher = new GitContentSearcher(gitHelperMock.Object, fileSearcherMock.Object);

            using (var stringWriter = new StringWriter())
            {
                // Act
                gitContentSearcher.SearchContent("dummy/path.txt", "search string", logWriter: stringWriter);

                // Assert
                var logContent = stringWriter.ToString();

                // Check the log to confirm that the string was not found in any commits
                Assert.Contains("Search string \"search string\" does not appear in any of the checked commits.", logContent);
            }
        }

        [Fact]
        public void SearchContent_ShouldLogCorrectly_WhenStringAppearsInSingleCommit()
        {
            // Arrange
            var gitHelperMock = new Mock<IGitHelper>();
            var fileSearcherMock = new Mock<IFileSearcher>();

            // Simulate commits
            var commits = new[] { "commit1", "commit2", "commit3", "commit4", "commit5" };
            gitHelperMock.Setup(g => g.GetGitCommits(It.IsAny<string>(), It.IsAny<string>())).Returns(commits);
            gitHelperMock.Setup(g => g.GetCommitTime(It.IsAny<string>())).Returns("2023-08-21 12:00:00");

            // Simulate string appearance: Found only in commit3
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit1")), It.IsAny<string>())).Returns(false);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit2")), It.IsAny<string>())).Returns(false);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit3")), It.IsAny<string>())).Returns(true);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit4")), It.IsAny<string>())).Returns(false);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit5")), It.IsAny<string>())).Returns(false);


            var gitContentSearcher = new GitContentSearcher(gitHelperMock.Object, fileSearcherMock.Object);

            using (var stringWriter = new StringWriter())
            {
                // Act
                gitContentSearcher.SearchContent("dummy/path.txt", "search string", logWriter: stringWriter);

                // Assert
                var logContent = stringWriter.ToString();

                // Check the log for the correct first and last appearance commits
                Assert.Contains("Search string \"search string\" first appears in commit commit3.", logContent);
                Assert.Contains("Search string \"search string\" last appears in commit commit3.", logContent);
            }
        }
    }
}
