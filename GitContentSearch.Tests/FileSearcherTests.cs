using Moq;
using NPOI.SS.UserModel;
using System;
using System.IO;
using Xunit;

namespace GitContentSearch.Tests
{
    public class FileSearcherTests
    {
        [Fact]
        public void IsTextFile_ShouldReturnTrue_ForTextFile()
        {
            // Arrange
            var fileSearcher = new FileSearcher();
            string tempFilePath = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempFilePath, "This is a simple text file.");

                // Act
                var result = fileSearcher.IsTextFile(tempFilePath);

                // Assert
                Assert.True(result);
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        [Fact]
        public void IsTextFile_ShouldReturnFalse_ForBinaryFile()
        {
            // Arrange
            var fileSearcher = new FileSearcher();
            string tempFilePath = Path.GetTempFileName();

            try
            {
                // Write binary content to the file
                byte[] binaryData = new byte[] { 0, 1, 2, 3, 4, 5 };
                File.WriteAllBytes(tempFilePath, binaryData);

                // Act
                var result = fileSearcher.IsTextFile(tempFilePath);

                // Assert
                Assert.False(result);
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        [Fact]
        public void SearchInTextFile_ShouldReturnTrue_WhenStringFoundInTextFile()
        {
            // Arrange
            var fileSearcher = new FileSearcher();
            string tempFilePath = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempFilePath, "This file contains the search string.");

                // Act
                var result = fileSearcher.SearchInTextFile(tempFilePath, "search string");

                // Assert
                Assert.True(result);
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        [Fact]
        public void SearchInTextFile_ShouldReturnFalse_WhenStringNotFoundInTextFile()
        {
            // Arrange
            var fileSearcher = new FileSearcher();
            string tempFilePath = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempFilePath, "This file does not contain the string.");

                // Act
                var result = fileSearcher.SearchInTextFile(tempFilePath, "missing string");

                // Assert
                Assert.False(result);
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
