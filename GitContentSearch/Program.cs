using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel; // For .xls
using NPOI.XSSF.UserModel; // For .xlsx

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: <program> <file-path> <search-string> [--earliest-commit=<commit>] [--latest-commit=<commit>]");
            return;
        }

        string filePath = args[0];
        string searchString = args[1];
        string earliestCommit = "";
        string latestCommit = "";

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
        }

        // Retrieve the list of commits
        var commits = GetGitCommits(earliestCommit, latestCommit);

        if (commits == null || commits.Length == 0)
        {
            Console.WriteLine("No commits found in the specified range.");
            return;
        }

        // Perform a binary search to find the commit where the string disappears
        int left = 0;
        int right = commits.Length - 1;

        using (var logFile = new StreamWriter("search_log.txt", append: true))
        {
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                string commit = commits[mid];

                string tempFileName = $"temp_{commit}{Path.GetExtension(filePath)}";

                // Execute the git show command directly and write output to the file
                try
                {
                    RunGitShow(commit, filePath, tempFileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving file at commit {commit}: {ex.Message}");
                    right = mid - 1;
                    continue;
                }

                // Search for the string in the file
                bool found = SearchInFile(tempFileName, searchString);

                // Get the commit time
                string commitTime;
                try
                {
                    commitTime = GetCommitTime(commit);
                }
                catch (Exception ex)
                {
                    commitTime = $"unknown time ({ex.Message})";
                }

                // Log the progress
                logFile.WriteLine($"Checked commit: {commit} at {commitTime}, found: {found}");
                logFile.Flush();

                if (found)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }

                // Clean up temporary file
                if (File.Exists(tempFileName))
                {
                    File.Delete(tempFileName);
                }
            }

            // Final output decision
            if (right < 0)
            {
                Console.WriteLine($"Search string \"{searchString}\" does not appear in any of the checked commits.");
            }
            else if (left >= commits.Length)
            {
                Console.WriteLine($"Search string \"{searchString}\" appears in all checked commits.");
            }
            else
            {
                Console.WriteLine($"Search string \"{searchString}\" appears in commit {commits[right]}.");
            }
        }
    }

    static string GetCommitTime(string commitHash)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"show -s --format=%ci {commitHash}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(startInfo))
        {
            if (process == null)
            {
                throw new Exception("Failed to start git process.");
            }

            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Error getting commit time: {error}");
            }

            return output;
        }
    }

    static void RunGitShow(string commit, string filePath, string outputFile)
    {
        // Ensure the file path is properly quoted
        string quotedFilePath = $"\"{filePath}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"show {commit}:{quotedFilePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(startInfo))
        {
            if (process == null)
            {
                throw new Exception("Failed to start git process.");
            }

            using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                process.StandardOutput.BaseStream.CopyTo(outputStream);
            }

            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Error running git show: {error}");
            }
        }
    }

    static string[] GetGitCommits(string earliest, string latest)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "log --pretty=format:%H",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(startInfo))
        {
            if (process == null)
            {
                Console.WriteLine("Failed to start git process.");
                return Array.Empty<string>();
            }

            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"Error retrieving git commits: {error}");
                return Array.Empty<string>();
            }

            var commits = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            return FilterCommitsByRange(commits, earliest, latest);
        }
    }

    static string[] FilterCommitsByRange(string[] commits, string earliest, string latest)
    {
        int startIndex = 0;
        int endIndex = commits.Length - 1;

        if (!string.IsNullOrEmpty(latest))
        {
            startIndex = Array.IndexOf(commits, latest);
            if (startIndex == -1)
            {
                Console.WriteLine($"Latest commit {latest} not found.");
                startIndex = 0;
            }
        }

        if (!string.IsNullOrEmpty(earliest))
        {
            endIndex = Array.IndexOf(commits, earliest);
            if (endIndex == -1)
            {
                Console.WriteLine($"Earliest commit {earliest} not found.");
                endIndex = commits.Length - 1;
            }
        }

        if (startIndex > endIndex)
        {
            Console.WriteLine("Invalid commit range specified.");
            return new string[0];
        }

        return commits.Skip(startIndex).Take(endIndex - startIndex + 1).ToArray();
    }

    static bool SearchInFile(string fileName, string searchString)
    {
        if (IsTextFile(fileName))
        {
            return SearchInTextFile(fileName, searchString);
        }
        else
        {
            return SearchInExcel(fileName, searchString);
        }
    }

    static bool SearchInTextFile(string fileName, string searchString)
    {
        try
        {
            foreach (var line in File.ReadLines(fileName))
            {
                if (line.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading text file {fileName}: {ex.Message}");
            return false;
        }

        return false;
    }

    static bool IsTextFile(string filePath)
    {
        const int sampleSize = 512;
        byte[] buffer = new byte[sampleSize];

        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            int bytesRead = fileStream.Read(buffer, 0, buffer.Length);

            for (int i = 0; i < bytesRead; i++)
            {
                byte b = buffer[i];
                if (b == 0) // Null byte found, not a text file
                {
                    return false;
                }
                // Check for non-printable characters except for newlines and carriage returns
                if (b < 32 && b != 9 && b != 10 && b != 13)
                {
                    return false;
                }
            }
        }
        return true;
    }

    static bool SearchInExcel(string fileName, string searchString)
    {
        try
        {
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook;
                string extension = Path.GetExtension(fileName).ToLower();

                if (extension == ".xls")
                {
                    workbook = new HSSFWorkbook(fileStream);
                }
                else if (extension == ".xlsx")
                {
                    workbook = new XSSFWorkbook(fileStream);
                }
                else
                {
                    Console.WriteLine($"Unsupported file extension: {extension}");
                    return false;
                }

                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    var sheet = workbook.GetSheetAt(i);
                    if (sheet == null) continue;

                    foreach (IRow row in sheet)
                    {
                        if (row == null) continue;

                        foreach (ICell cell in row.Cells)
                        {
                            if (cell == null) continue;

                            string cellValue = GetCellValueAsString(cell);
                            if (cellValue != null && cellValue.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading Excel file {fileName}: {ex.Message}");
            return false;
        }

        return false;
    }

    static string GetCellValueAsString(ICell cell)
    {
        switch (cell.CellType)
        {
            case CellType.String:
                return cell.StringCellValue;
            case CellType.Numeric:
                if (DateUtil.IsCellDateFormatted(cell))
                    return cell.DateCellValue?.ToString() ?? string.Empty;
                else
                    return cell.NumericCellValue.ToString();
            case CellType.Boolean:
                return cell.BooleanCellValue.ToString();
            case CellType.Formula:
                return cell?.ToString() ?? string.Empty; // Evaluating formulas can be complex; for simplicity, using the cached value
            case CellType.Blank:
                return string.Empty;
            default:
                return cell?.ToString() ?? string.Empty;
        }
    }
}
