using System;
using Xunit;

namespace GitContentSearch.Tests
{
    public class GitHelperTests
    {
        [Fact]
        public void GetGitCommits_ShouldReturnEmptyArray_OnGitFailure()
        {
            // Arrange
            var gitHelper = new GitHelper();

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
            var gitHelper = new GitHelper();

            // Act & Assert
            Assert.Throws<Exception>(() => gitHelper.GetCommitTime("invalidCommit"));
        }
    }
}
