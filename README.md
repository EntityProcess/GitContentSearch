# GitContentSearch

## Overview

**GitContentSearch** is a command-line tool written in C# that searches for specific content within files across different Git commits. The app supports searching within Content files (.xls, .xlsx) and text files (.txt, .sql, .cs, etc.). It efficiently identifies the commit where the search string appears, using a binary search algorithm for faster results. Progress is logged to a file, allowing you to resume the search if interrupted.

## Features

- **Binary Search**: Quickly identifies the commit where the search string first appears and last disappears.
- **Supports Large Repos**: Handles extensive commit histories by narrowing down the search space.
- **Log File**: Keeps track of all checked commits and results, allowing you to continue the search later.
- **Commit Range**: Specify an earliest and latest commit to limit the search scope.

## Installation

**Clone the repository**:

```bash
git clone https://github.com/entityprocess/GitContentSearch.git
cd GitContentSearch
```

**Build the application**:

```bash
dotnet publish -c Release
```

**Add the executable folder to your PATH environment variable**

After building the application, you will want to add the folder containing the GitContentSearch.exe (or equivalent for your operating system) to your PATH environment variable. This allows you to run the tool from any directory in your command line.

```
/path/to/your/git/repository/GitContentSearch/bin/Release/netX.X/publish/
```

## Usage

**1. Navigate to your Git directory**:
  
Before running the tool, ensure you're in the directory where your Git repository is located:

```bash
cd /path/to/your/git/repository
```
**2. Run the tool**:

```bash
GitContentSearch.exe <file-path> <search-string> [--earliest-commit=<commit>] [--latest-commit=<commit>] [--working-directory=<path>] [--log-directory=<path>]
```

Note: Make sure the file path does not start with a forward slash (/), as this can cause errors when Git tries to locate the file.

### Arguments

* `<file-path>`: The path to the Content file within the Git repository.
* `<search-string>`: The string you want to search for in the Content file.
* `--earliest-commit=<commit>`: (Optional) The earliest commit to begin the search.
* `--latest-commit=<commit>`: (Optional) The latest commit to end the search.
* `--working-directory=<path>`: (Optional) The directory where Git commands should be executed. Defaults to the user's temp directory if not provided.
* `--log-directory=<path>`: (Optional) The directory where the log file and temporary files will be stored. Defaults to the user's temp directory if not provided.

### Example

```bash
GitContentSearch.exe "path/to/your/Content-file.xlsx" "SearchString" --earliest-commit=abc123 --latest-commit=def456 --working-directory="/your/git/repo" --log-directory="/your/log/directory"
```

This will search for the string "SearchString" within the specified commit range, using the specified working directory for Git operations and storing logs and temporary files in the specified log directory.

## Output

Search Log: A file named search_log.txt is created in the log directory, detailing the commits checked and whether the string was found. The log directory is also used to store any temporary files generated during the search process.

## Dependencies

* NPOI: [NPOI](https://github.com/nissl-lab/npoi) is a .NET library that provides support for reading and writing Microsoft Office formats, including Content (.xls, .xlsx). It's used in this project to process Content files.
* Git: Make sure Git is installed on your system, as the app relies on the git show command.

## Contributing

Contributions are welcome! Please feel free to submit a pull request or open an issue for any bugs or feature requests.
