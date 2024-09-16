using GitContentSearch.Helpers;
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
            var commits = new[] { "commit5", "commit4", "commit3", "commit2", "commit1" };
            gitHelperMock.Setup(g => g.GetGitCommits(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(commits);
            gitHelperMock.Setup(g => g.GetCommitTime(It.IsAny<string>())).Returns("2023-08-21 12:00:00");

            // Simulate string appearances based on the specific commit
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit1")), It.IsAny<string>())).Returns(false);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit2")), It.IsAny<string>())).Returns(true);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit3")), It.IsAny<string>())).Returns(true);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit4")), It.IsAny<string>())).Returns(true);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit5")), It.IsAny<string>())).Returns(false);

            using (var stringWriter = new StringWriter())
            {
                // Act
                var gitContentSearcher = new GitContentSearcher(gitHelperMock.Object, fileSearcherMock.Object, new FileManager(), disableLinearSearch: true, logWriter: stringWriter);
                gitContentSearcher.SearchContent("dummy/path.txt", "search string");

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
            gitHelperMock.Setup(g => g.GetGitCommits(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(commits);
            gitHelperMock.Setup(g => g.GetCommitTime(It.IsAny<string>())).Returns("2023-08-21 12:00:00");

            // Simulate no appearances of the string
            fileSearcherMock.Setup(f => f.SearchInFile(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            using (var stringWriter = new StringWriter())
            {
                // Act
                var gitContentSearcher = new GitContentSearcher(gitHelperMock.Object, fileSearcherMock.Object, new FileManager(), disableLinearSearch: true, logWriter: stringWriter);
                gitContentSearcher.SearchContent("dummy/path.txt", "search string");

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
            gitHelperMock.Setup(g => g.GetGitCommits(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(commits);
            gitHelperMock.Setup(g => g.GetCommitTime(It.IsAny<string>())).Returns("2023-08-21 12:00:00");

            // Simulate string appearance: Found only in commit3
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit1")), It.IsAny<string>())).Returns(false);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit2")), It.IsAny<string>())).Returns(false);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit3")), It.IsAny<string>())).Returns(true);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit4")), It.IsAny<string>())).Returns(false);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit5")), It.IsAny<string>())).Returns(false);

            using (var stringWriter = new StringWriter())
            {
                // Act
                var gitContentSearcher = new GitContentSearcher(gitHelperMock.Object, fileSearcherMock.Object, new FileManager(), disableLinearSearch: true, logWriter: stringWriter);
                gitContentSearcher.SearchContent("dummy/path.txt", "search string");

                // Assert
                var logContent = stringWriter.ToString();

                // Check the log for the correct first and last appearance commits
                Assert.Contains("Search string \"search string\" first appears in commit commit3.", logContent);
                Assert.Contains("Search string \"search string\" last appears in commit commit3.", logContent);
            }
        }

        [Fact]
        public void SearchContent_ShouldRestrictSearchToSpecifiedCommitRange()
        {
            // Arrange
            var gitHelperMock = new Mock<IGitHelper>();
            var fileSearcherMock = new Mock<IFileSearcher>();

            // Simulate a broader range of commits
            var allCommits = new[] { "commit5", "commit4", "commit3", "commit2", "commit1" };
            var restrictedCommits = new[] { "commit3", "commit2" };

            // Mock the GetGitCommits method to return only the restricted range when specified
            gitHelperMock.Setup(g => g.GetGitCommits("commit2", "commit3", It.IsAny<string>())).Returns(restrictedCommits);
            gitHelperMock.Setup(g => g.GetGitCommits(It.Is<string>(s => s != "commit2"), It.Is<string>(s => s != "commit3"), It.IsAny<string>())).Returns(allCommits);

            gitHelperMock.Setup(g => g.GetCommitTime(It.IsAny<string>())).Returns("2023-08-21 12:00:00");

            // Simulate string appearances based on the specific commit
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit1")), It.IsAny<string>())).Returns(false);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit2")), It.IsAny<string>())).Returns(true);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit3")), It.IsAny<string>())).Returns(false);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit4")), It.IsAny<string>())).Returns(false);
            fileSearcherMock.Setup(f => f.SearchInFile(It.Is<string>(file => file.Contains("commit5")), It.IsAny<string>())).Returns(false);

            using (var stringWriter = new StringWriter())
            {
                // Act
                var gitContentSearcher = new GitContentSearcher(gitHelperMock.Object, fileSearcherMock.Object, new FileManager(), disableLinearSearch: true, logWriter: stringWriter);
                gitContentSearcher.SearchContent("dummy/path.txt", "search string", "commit2", "commit3");

                // Assert
                var logContent = stringWriter.ToString();

                // Ensure only the specified commit range was searched
                Assert.Contains("Checked commit: commit3", logContent);
                Assert.Contains("Checked commit: commit2", logContent);
                Assert.DoesNotContain("Checked commit: commit1", logContent);
                Assert.DoesNotContain("Checked commit: commit4", logContent);
                Assert.DoesNotContain("Checked commit: commit5", logContent);

                // Check the log for the correct first and last appearance commits within the range
                Assert.Contains("Search string \"search string\" first appears in commit commit2.", logContent);
                Assert.Contains("Search string \"search string\" last appears in commit commit2.", logContent);
            }
        }

        [Fact]
        public void SearchContent_ShouldLogError_WhenEarliestCommitIsMoreRecentThanLatestCommit()
        {
            // Arrange
            var gitHelperMock = new Mock<IGitHelper>();
            var fileSearcherMock = new Mock<IFileSearcher>();

            // Simulate commits in descending order as returned by Git
            var allCommits = new[] { "commit5", "commit4", "commit3", "commit2", "commit1" };

            // Mock the GetGitCommits method
            gitHelperMock.Setup(g => g.GetGitCommits(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(allCommits);
            gitHelperMock.Setup(g => g.GetCommitTime(It.IsAny<string>())).Returns("2023-08-21 12:00:00");

            using (var stringWriter = new StringWriter())
            {
                // Act
                var gitContentSearcher = new GitContentSearcher(gitHelperMock.Object, fileSearcherMock.Object, new FileManager(), disableLinearSearch: true, logWriter: stringWriter);
                gitContentSearcher.SearchContent("dummy/path.txt", "search string", "commit4", "commit3");

                // Assert
                var logContent = stringWriter.ToString();

                // Ensure that an error message is logged
                Assert.Contains("Error: The earliest commit is more recent than the latest commit.", logContent);
            }
        }
    }
}