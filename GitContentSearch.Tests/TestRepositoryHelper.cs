using LibGit2Sharp;
using System;
using System.IO;

namespace GitContentSearch.Tests
{
    public class TestRepositoryHelper : IDisposable
    {
        public string RepositoryPath { get; }
        private readonly Repository _repository;
        private bool _disposed;

        public TestRepositoryHelper()
        {
            // Create a temporary directory for the test repository
            RepositoryPath = Path.Combine(Path.GetTempPath(), "GitContentSearch_TestRepo_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(RepositoryPath);
            Repository.Init(RepositoryPath);
            
            // Initialize repository (this will throw if initialization fails, which is what we want in tests)
            _repository = new Repository(RepositoryPath);

            try
            {
                // Set up the test identity for commits
                _repository.Config.Set("user.name", "Test User");
                _repository.Config.Set("user.email", "test@example.com");
            }
            catch (Exception)
            {
                // If config setup fails, ensure we clean up the repository
                _repository.Dispose();
                throw;
            }
        }

        public string CreateFile(string fileName, string content)
        {
            ThrowIfDisposed();
            string filePath = Path.Combine(RepositoryPath, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        public string CreateAndCommitFile(string fileName, string content, string message = "Add test file")
        {
            ThrowIfDisposed();
            string filePath = CreateFile(fileName, content);
            
            Commands.Stage(_repository, fileName);
            var author = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
            _repository.Commit(message, author, author);

            return filePath;
        }

        public string GetLastCommitHash()
        {
            ThrowIfDisposed();
            return _repository.Head.Tip.Sha;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TestRepositoryHelper));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _repository.Dispose();
                }

                // Clean up the test repository directory
                if (Directory.Exists(RepositoryPath))
                {
                    try
                    {
                        Directory.Delete(RepositoryPath, true);
                    }
                    catch
                    {
                        // Best effort cleanup
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
} 