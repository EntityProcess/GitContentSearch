using GitContentSearch.Helpers;
using GitContentSearch.Interfaces;
using System.IO;

namespace GitContentSearch
{
	class Program
	{
		private static string SetupTempDirectory(string? logDirectory = null)
		{
			var tempDir = logDirectory ?? Path.Combine(Path.GetTempPath(), "GitContentSearch");
			if (!Directory.Exists(tempDir))
			{
				Directory.CreateDirectory(tempDir);
			}
			return tempDir;
		}

		private static ISearchLogger CreateLogger(string tempDir)
		{
			var fileWriter = new StreamWriter(Path.Combine(tempDir, "search_log.txt"), append: true);
			var compositeWriter = new CompositeTextWriter(Console.Out, fileWriter);
			return new SearchLogger(compositeWriter);
		}

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Usage:");
				Console.WriteLine("  Search: <program> <file-path> <search-string> [--earliest-commit=<commit>] [--latest-commit=<commit>] [--working-directory=<path>] [--log-directory=<path>] [--follow]");
				Console.WriteLine("  Locate: <program> --locate-only <file-name>");
				return;
			}

			// Handle locate-only mode
			if (args[0] == "--locate-only")
			{
				if (args.Length != 2)
				{
					Console.WriteLine("Usage for locate: <program> --locate-only <file-name>");
					return;
				}

				string fileName = args[1];
				string locateWorkingDir = Directory.GetCurrentDirectory();
				string locateTempDir = SetupTempDirectory();

				using (var logger = CreateLogger(locateTempDir))
				{
					logger.LogHeader("locate", locateWorkingDir, fileName);

					var processWrapper = new ProcessWrapper();
					var gitHelper = new GitHelper(processWrapper, locateWorkingDir, false, logger);
					var gitLocator = new GitLocator(gitHelper, logger, processWrapper);
					gitLocator.LocateFile(fileName);

					logger.LogFooter();
				}
				return;
			}

			// Original search functionality
			if (args.Length < 2)
			{
				Console.WriteLine("Usage for search: <program> <file-path> <search-string> [--earliest-commit=<commit>] [--latest-commit=<commit>] [--working-directory=<path>] [--log-directory=<path>] [--follow]");
				return;
			}

			string filePath = args[0];
			string searchString = args[1];
			string earliestCommit = "";
			string latestCommit = "";
			bool follow = false;
			string? workingDirectory = null;
			string? logDirectory = null;

			// Parse optional arguments
			foreach (var arg in args.Skip(2))
			{
				if (arg.StartsWith("--earliest-commit="))
				{
					earliestCommit = arg.Replace("--earliest-commit=", "");
				}
				else if (arg.StartsWith("--latest-commit="))
				{
					latestCommit = arg.Replace("--latest-commit=", "");
				}
				else if (arg.StartsWith("--working-directory="))
				{
					workingDirectory = arg.Replace("--working-directory=", "");
				}
				else if (arg.StartsWith("--log-directory="))
				{
					logDirectory = arg.Replace("--log-directory=", "");
				}
				else if (arg == "--follow")
				{
					follow = true;
				}
			}

			workingDirectory ??= Directory.GetCurrentDirectory();
			string tempDir = SetupTempDirectory(logDirectory);

			using (var logger = CreateLogger(tempDir))
			{
				logger.LogHeader("search", workingDirectory, filePath);

				var processWrapper = new ProcessWrapper();
				var gitHelper = new GitHelper(processWrapper, workingDirectory, follow, logger);
				var fileSearcher = new FileSearcher();
				var fileManager = new FileManager(tempDir);
				var gitContentSearcher = new GitContentSearcher(gitHelper, fileSearcher, fileManager, logger);

				gitContentSearcher.SearchContent(filePath, searchString, earliestCommit, latestCommit);

				logger.LogFooter();
			}
		}
	}
}
