# Product Requirements Document: Date-Based Search in GitContentSearch

## 1. Introduction

This document outlines the requirements for adding date-based search functionality to GitContentSearch. Currently, the tool allows searching for a string within a file's history between two specified commit SHAs. This enhancement will enable users to search between two dates, making it more intuitive and user-friendly.

## 2. How It Works

The date-based search functionality will be implemented as follows:

1.  **Date to Commit Mapping:**
    *   GitContentSearch will use the `git log` command to identify commits within the specified date range.
    *   The command `git log --since="start-date" --until="end-date" --pretty=%H` will be executed to retrieve a list of commit SHAs.
        *   `--since`: Specifies the start date.
        *   `--until`: Specifies the end date.
        *   `--pretty=%H`: Formats the output to only include the commit SHA.
    *   Supported date formats include `YYYY-MM-DD` (e.g., `2023-01-01`) and relative dates (e.g., `2.weeks.ago`).
    *   The earliest commit after `--since` will be considered the "earliest commit".
    *   The latest commit before `--until` will be considered the "latest commit".

2.  **Binary Search:** Once the earliest and latest commits are determined, the existing binary search algorithm will be used to find the commit where the search string was introduced.

## 3. User Interface (UI) Changes

*   **Input Fields:**
    *   Replace the existing "Earliest Commit" and "Latest Commit" input fields with "Start Date" and "End Date" fields.
*   **Date Picker:**
    *   Utilize a date picker widget (e.g., from AvaloniaUI or a similar cross-platform framework) to provide a user-friendly date selection experience.
*   **Validation:**
    *   Implement input validation to ensure:
        *   The start date is before the end date.
        *   Both dates are in a valid format (e.g., `YYYY-MM-DD`).
        *   Dates are not in the future.

## 4. Command-Line Interface (CLI) Changes

*   **Backward Compatibility:**
    *   Retain the existing `--earliest-commit` and `--latest-commit` options for backward compatibility.
*   **New Date Options:**
    *   Introduce new optional arguments:
        *   `--start-date`: Specifies the start date for the search.
        *   `--end-date`: Specifies the end date for the search.
*   **Example Command:**
    ```bash
    GitContentSearch.exe "path/to/your/file.xlsx" "SearchString" --start-date="2023-01-01" --end-date="2023-12-31" --working-directory="/your/git/repo" --log-directory="/your/log/directory" --follow
    ```
* **Option Priority:**
    *   If both commit-based (`--earliest-commit`, `--latest-commit`) and date-based (`--start-date`, `--end-date`) options are provided, prioritize commit-based options.  Log a warning message indicating this prioritization.

## 5. Edge Case Handling

*   **No Commits in Range:**
    *   If no commits are found within the specified date range, display a clear and informative error message to the user.  Example: "No commits found between 2023-01-01 and 2023-12-31".
*   **Time Zones:**
    *   Consistently use UTC (Git's default behavior) for date and time comparisons.
    *   Include a note in both the UI and CLI documentation clarifying the time zone handling (UTC).
*   **Invalid Dates:**
    *   Implement robust date validation to prevent errors.  This includes:
        *   Rejecting dates in the future.
        *   Ensuring the date format is correct (e.g., `YYYY-MM-DD`).
        *   Handling invalid date inputs gracefully (e.g., February 30th).
