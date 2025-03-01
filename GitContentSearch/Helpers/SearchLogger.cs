using GitContentSearch.Interfaces;
using System;
using System.IO;

namespace GitContentSearch.Helpers
{
    public class SearchLogger : ISearchLogger
    {
        private readonly TextWriter _writer;
        private readonly Action<string>? _progressCallback;
        private bool _disposedValue;

        /// <summary>
        /// Event that fires when a log message is added
        /// </summary>
        public event EventHandler<string>? LogAdded;

        /// <summary>
        /// Gets the underlying TextWriter
        /// </summary>
        public TextWriter Writer => _writer;

        public SearchLogger(TextWriter writer, Action<string>? progressCallback = null)
        {
            _writer = writer;
            _progressCallback = progressCallback;
        }

        public void LogHeader(string operation, string workingDirectory, string targetFile, string? tempDirectory = null)
        {
            var divider = new string('=', 50);
            _writer.WriteLine(divider);
            _writer.WriteLine($"GitContentSearch {operation} started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer.WriteLine($"Working Directory (Git Repo): {workingDirectory}");
            _writer.WriteLine($"File to {operation.ToLower()}: {targetFile}");
            
            // Use the provided temp directory or default to the standard path
            string tempDir = tempDirectory ?? Path.Combine(Path.GetTempPath(), "GitContentSearch");
            _writer.WriteLine($"Logs and temporary files will be created in: {tempDir}");
            
            _writer.WriteLine(divider);
            
            // Trigger LogAdded event for each line
            LogAdded?.Invoke(this, divider);
            LogAdded?.Invoke(this, $"GitContentSearch {operation} started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogAdded?.Invoke(this, $"Working Directory (Git Repo): {workingDirectory}");
            LogAdded?.Invoke(this, $"File to {operation.ToLower()}: {targetFile}");
            LogAdded?.Invoke(this, $"Logs and temporary files will be created in: {tempDir}");
            LogAdded?.Invoke(this, divider);
        }

        public void LogProgress(string progressMessage)
        {
            if (_progressCallback != null)
            {
                _progressCallback(progressMessage);
            }
            else
            {
                Console.Write($"\r{progressMessage}".PadRight(50));
            }
            LogAdded?.Invoke(this, progressMessage);
        }

        public void LogFooter()
        {
            if (_progressCallback == null)
            {
                Console.WriteLine(); // Clear progress line in CLI mode
            }
            var divider = new string('=', 50);
            _writer.WriteLine(divider);
            LogAdded?.Invoke(this, divider);
        }

        public void LogError(string message, Exception? ex = null)
        {
            _writer.WriteLine($"Error: {message}");
            if (ex?.InnerException != null)
            {
                _writer.WriteLine($"Inner Error: {ex.InnerException.Message}");
            }
            LogAdded?.Invoke(this, $"Error: {message}");
            if (ex?.InnerException != null)
            {
                LogAdded?.Invoke(this, $"Inner Error: {ex.InnerException.Message}");
            }
        }

        public void WriteLine(string message)
        {
            _writer.WriteLine(message);
            LogAdded?.Invoke(this, message);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_writer is IDisposable disposableWriter)
                    {
                        disposableWriter.Dispose();
                    }
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

		public void Flush()
		{
            _writer.Flush();
		}
	}
} 