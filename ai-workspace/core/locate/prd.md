**Product Requirements Document (PRD): Locate File in Git History**

---

### **Overview**
This feature will add the ability to locate the most recent commit that contains a specified file in a Git repository. It will be implemented both in the **CLI** (via `--locate-only` argument) and the **GUI** (as a "Locate" button in the Avalonia UI). The feature will use **LibGit2Sharp** to ensure cross-platform compatibility.

---

### **Goals & Objectives**
- Enable users to search for a file and retrieve the full path of the most recent commit containing it.
- Ensure feature parity between the CLI and GUI implementations.
- Maintain cross-platform support (Windows, macOS, Linux) using **LibGit2Sharp**.
- Provide a user-friendly interface for the GUI while keeping the CLI efficient.

---

### **Functional Requirements**

#### **1. CLI: `--locate-only` Argument**
- Users can run the CLI with:
  ```sh
  GitContentSearch --locate-only "Schema_Main.sql"
  ```
- The program should:
  1. Search for the latest commit containing the file.
  2. Return the **full file path** from the repository root.
  3. Print a message if the file is not found.
- Expected Output Example:
  ```sh
  Found in commit 1a2b3c4d5e6f: src/database/Schema_Main.sql
  ```

#### **2. GUI: "Locate" Button in Avalonia UI**
- Add a **"Locate"** button next to the existing search functionality.
- When clicked:
  1. A file selection field allows users to input a filename.
  2. The system searches for the latest commit that contains the file.
  3. The full file path is displayed in a UI text box.
  4. If not found, display a user-friendly message.
- The button should be disabled while searching and re-enabled upon completion.

---

### **Technical Implementation**

#### **LibGit2Sharp Integration**
- Use `Repository.Commits` to iterate through the commit history.
- Traverse the commit tree to check if the file exists.
- Retrieve the most recent commit where the file appears.

#### **Performance Considerations**
- Optimize search by scanning commits in reverse chronological order.
- Cache results when applicable to avoid redundant searches.

#### **Error Handling**
- If the repository is invalid, display an error message.
- If the file does not exist in any commit, return a clear message.

---

### **User Experience (UX) Considerations**
- **CLI:** Provide clear output with minimal clutter.
- **GUI:** Ensure smooth interaction with proper loading indicators.

---

### **Testing & Validation**
- **Unit Tests:** Ensure file location works correctly.
- **Integration Tests:** Verify both CLI and GUI behaviors.
- **Cross-Platform Testing:** Validate on Windows, macOS, and Linux.

---

### **Release Plan**
- **Phase 1:** Implement CLI `--locate-only` functionality.
- **Phase 2:** Develop and integrate the "Locate" button in the GUI.
- **Phase 3:** Test, refine, and release the feature.


