using GitContentSearch.Helpers;
using System;

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
            }

            var logWriter = new CompositeTextWriter(
                Console.Out,
                new StreamWriter(Path.Combine(logDirectory ?? Directory.GetCurrentDirectory(), "search_log.txt"), append: true)
                );

            var processWrapper = new ProcessWrapper();
            var gitHelper = new GitHelper(processWrapper, workingDirectory);
            var fileSearcher = new FileSearcher();
            var fileManager = new FileManager(logDirectory);
            var gitContentSearcher = new GitContentSearcher(gitHelper, fileSearcher, fileManager, logWriter);
            gitContentSearcher.SearchContent(filePath, searchString, earliestCommit, latestCommit);
        }
    }
}
