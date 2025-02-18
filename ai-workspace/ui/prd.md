## PRD: GitContentSearch Graphical User Interface (GUI)

**Goal:** To create a user-friendly graphical interface for GitContentSearch, simplifying its usage and making it accessible to users who prefer not to use the command line.

**Target User:**  Developers, project managers, and any users who need to search for content within Git repositories but find the command-line interface cumbersome. The UI should be intuitive for both technical and less technical users.

**Problem Statement:** The current command-line interface (CLI) for GitContentSearch can be intimidating and complex for some users. Remembering command syntax and options reduces usability and accessibility.

**Proposed Solution:** Develop a graphical user interface (GUI) application using AvaloniaUI to provide an intuitive and visual way to interact with GitContentSearch. This UI will abstract the complexities of the command line, making the tool easier to use.

**Key Features:**

1.  **File Path Input:**
    *   Allow users to specify the file path within the Git repository to search.
    *   Include a "Browse" button to visually select the file.
2.  **Search String Input:**
    *   Provide a text field for users to enter the search string.
3.  **Commit Range Selection (Optional):**
    *   Fields to specify "Earliest Commit" and "Latest Commit" for targeted searches.
    *   Clear labels and tooltips to explain commit hash or branch name input.
4.  **Working Directory Selection (Optional):**
    *   Allow users to specify the Git repository's working directory.
    *   Include a "Browse" button for directory selection.
5.  **Log Directory Selection (Optional):**
    *   Allow users to customize the log directory.
    *   Include a "Browse" button for directory selection.
6.  **Options:**
    *   **Disable Linear Search Checkbox:** Expose the option to disable linear search with a clear description of its impact.
    *   **Follow History Checkbox:**  Expose the `--follow` option with a clear description.
7.  **Start Search Button:**
    *   A button to initiate the Git content search process.
8.  **Progress Indication:**
    *   Display a progress bar to show the status of the search operation.
9.  **Real-time Log Output Display:**
    *   Display the application log output in a text box within the UI as the search progresses. This should include commit checks and search results in real-time.

**UI Elements:**

*   **File Path Input:**  Labelled Text Box with "Browse" button.
*   **Search String Input:** Labelled Text Box.
*   **Earliest Commit Input:** Labelled Text Box.
*   **Latest Commit Input:** Labelled Text Box.
*   **Working Directory Input:** Labelled Text Box with "Browse" button.
*   **Log Directory Input:** Labelled Text Box with "Browse" button.
*   **Checkboxes:** "Disable Linear Search", "Follow History" with labels.
*   **Button:** "Start Search" (prominent).
*   **Progress Bar:** Below the "Start Search" button.
*   **Log Display:** Read-only Text Box, scrollable, for log output.

**Code Refactoring for Logging:**

*   **Requirement:**  Refactor the logging mechanism to enable real-time display of logs in the UI.
*   **Investigation:** Explore using a suitable .NET logging library (e.g., Serilog, NLog) that allows for:
    *   Directing log output to multiple targets (UI text box and log file).
    *   Asynchronous logging to prevent UI blocking.
    *   Potentially using events or observable patterns to push log messages to the UI for display as they are generated during the search process.

**Success Metrics:**

*   Users can easily initiate and monitor Git content searches through the UI.
*   Reduced user errors compared to command-line usage.
*   Positive user feedback on ease of use and clarity of the UI.

This PRD provides a concise overview for developing a user-friendly GUI for GitContentSearch, addressing the key features, UI elements, and the necessary logging considerations for real-time UI updates. Let me know if you would like any modifications or further details!