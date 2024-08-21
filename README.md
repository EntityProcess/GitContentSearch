# GitExcelSearch

## Overview

**GitExcelSearch** is a command-line tool written in Go that searches for a specific string in Excel files across different Git commits. The app efficiently identifies the commit where the search string no longer appears, using a binary search algorithm for faster results. Progress is logged to a file, allowing you to resume the search if interrupted.

## Features

- **Binary Search**: Quickly identifies the commit where the search string disappears.
- **Supports Large Repos**: Handles extensive commit histories by narrowing down the search space.
- **Log File**: Keeps track of all checked commits and results, allowing you to continue the search later.
- **Commit Range**: Specify an earliest and latest commit to limit the search scope.

## Installation

Clone the repository:

```bash
git clone https://github.com/entityprocess/GitExcelSearch.git
cd GitExcelSearch
```

Build the application:

```bash
dotnet build
```

## Usage

```bash
GitExcelSearch.exe <file-path> <search-string> [--earliest-commit=<commit>] [--latest-commit=<commit>]
```

### Arguments

* `<file-path>`: The path to the Excel file within the Git repository.
* `<search-string>`: The string you want to search for in the Excel file.
* `--earliest-commit=<commit>`: (Optional) The earliest commit to begin the search.
* `--latest-commit=<commit>`: (Optional) The latest commit to end the search.

### Example

```bash
GitExcelSearch.exe "path/to/your/excel-file.xlsx" "SearchString" --earliest-commit=abc123 --latest-commit=def456
```

This will search for the string "SearchString" within the specified commit range.

## Output

Search Log: A file named search_log.txt is created in the working directory, detailing the commits checked and whether the string was found.

## Dependencies

* NPOI: [NPOI](https://github.com/nissl-lab/npoi) is a .NET library that provides support for reading and writing Microsoft Office formats, including Excel (.xls, .xlsx). It's used in this project to process Excel files.
* Git: Make sure Git is installed on your system, as the app relies on the git show command.

## Contributing

Contributions are welcome! Please feel free to submit a pull request or open an issue for any bugs or feature requests.
