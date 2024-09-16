using GitContentSearch.Helpers;
using System.IO;

namespace GitContentSearch
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: <program> <file-path> <search-string> [--earliest-commit=<commit>] [--latest-commit=<commit>]  [--working-directory=<path>] [--log-directory=<path>]");
                return;
            }

            string filePath = args[0];
            string searchString = args[1];
            string earliestCommit = "";
            string latestCommit = "";
            bool disableLinearSearch = false;
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
                else if (arg == "--disable-linear-search")
                {
                    disableLinearSearch = true;
                }
            }

            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = Directory.GetCurrentDirectory();
            }

            string logAndTempFileDirectory = logDirectory ?? string.Empty;
            if (string.IsNullOrEmpty(logAndTempFileDirectory))
            {
                string tempPath = Path.GetTempPath();
                logAndTempFileDirectory = Path.Combine(tempPath, "GitContentSearch");

                if (!Directory.Exists(logAndTempFileDirectory))
                {
                    Directory.CreateDirectory(logAndTempFileDirectory);
                }
            }

            using (var logWriter = new CompositeTextWriter(
                Console.Out,
                new StreamWriter(Path.Combine(logAndTempFileDirectory, "search_log.txt"), append: true)))
            {
                logWriter.WriteLine(new string('=', 50));
                logWriter.WriteLine($"GitContentSearch started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logWriter.WriteLine($"Working Directory (Git Repo): {workingDirectory ?? "Not specified, using current directory"}");
                logWriter.WriteLine($"Logs and temporary files will be created in: {logAndTempFileDirectory}");
                logWriter.WriteLine(new string('=', 50));

                var processWrapper = new ProcessWrapper();
                var gitHelper = new GitHelper(processWrapper, workingDirectory);
                var fileSearcher = new FileSearcher();
                var fileManager = new FileManager(logAndTempFileDirectory);
                var gitContentSearcher = new GitContentSearcher(gitHelper, fileSearcher, fileManager, disableLinearSearch, logWriter);

                gitContentSearcher.SearchContent(filePath, searchString, earliestCommit, latestCommit);

                logWriter.WriteLine($"GitContentSearch completed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logWriter.WriteLine(new string('=', 50));
            }
        }
    }
}
