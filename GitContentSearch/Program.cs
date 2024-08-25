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
                Console.WriteLine("Usage: <program> <file-path> <search-string> [--earliest-commit=<commit>] [--latest-commit=<commit>]  [--working-directory=<path>]");
                return;
            }

            string filePath = args[0];
            string searchString = args[1];
            string earliestCommit = "";
            string latestCommit = "";
            string? workingDirectory = null;

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
            }

            var processWrapper = new ProcessWrapper();
            var gitHelper = new GitHelper(processWrapper, workingDirectory);
            var fileSearcher = new FileSearcher();
            var gitContentSearcher = new GitContentSearcher(gitHelper, fileSearcher, new FileManager());
            gitContentSearcher.SearchContent(filePath, searchString, earliestCommit, latestCommit);
        }
    }
}
