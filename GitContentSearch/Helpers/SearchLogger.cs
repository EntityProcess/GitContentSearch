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

        public SearchLogger(TextWriter writer, Action<string>? progressCallback = null)
        {
            _writer = writer;
            _progressCallback = progressCallback;
        }

        public void LogHeader(string operation, string workingDirectory, string targetFile)
        {
            var divider = new string('=', 50);
            _writer.WriteLine(divider);
            _writer.WriteLine($"GitContentSearch {operation} started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer.WriteLine($"Working Directory (Git Repo): {workingDirectory}");
            _writer.WriteLine($"File to {operation.ToLower()}: {targetFile}");
            _writer.WriteLine(divider);
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
        }

        public void LogFooter()
        {
            if (_progressCallback == null)
            {
                Console.WriteLine(); // Clear progress line in CLI mode
            }
            _writer.WriteLine(new string('=', 50));
        }

        public void LogError(string message, Exception? ex = null)
        {
            _writer.WriteLine($"Error: {message}");
            if (ex?.InnerException != null)
            {
                _writer.WriteLine($"Inner Error: {ex.InnerException.Message}");
            }
        }

        public void WriteLine(string message)
        {
            _writer.WriteLine(message);
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