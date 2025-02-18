**Product Requirements Document (PRD): Soft Follow Feature for Code Tracking**

**1. Overview**
The **Soft Follow** feature extends `git --follow` by intelligently tracking code snippets across file renames and refactors. Instead of relying solely on Git's rename detection, it uses **user-defined heuristics** and **AI-based similarity scoring** to determine if a code snippet has been moved elsewhere in the repository. The tool aims to provide a more robust way to track code and offer insights even when files are heavily refactored.

**2. Objective**
The primary objectives are:
- Detect when a code snippet has been moved across files rather than deleted.
- Use **git diff** to find candidate files containing the snippet in the relevant commit.
- Rank candidate files by measuring **contextual similarity** between old and new locations.
- Provide user-friendly output about likely migration paths.
- Maintain compatibility across different file formats, including legacy formats.

**3. Implementation Details**

### **Phase 1: Core Text Search Implementation**

The initial implementation will focus on text-based files only, with the following steps:

1. **Ripgrep Integration**
   - Check if ripgrep exists in the system path or `tools/` directory
   - If not found, download the appropriate version from GitHub releases
   - Verify the installation and make ripgrep available for subsequent steps

2. **File Type Support Notice**
   - Display clear message that --soft-follow currently supports text files only
   - Binary files (e.g., .xls) support will be implemented in a future phase
   - Log unsupported file types when encountered

3. **Detect Snippet Absence**
   - Check whether specified snippet exists in the given file at a particular commit
   - Use git show and ripgrep to verify snippet presence/absence
   - If snippet is missing, proceed to candidate search

4. **Identify Candidate Files**
   - Use git diff to find modified files between commits
   - Search for the snippet in all modified files at the new commit
   - Generate a list of potential files containing the snippet

5. **Interactive File Selection**
   - Present candidate files to user in both CLI and UI
   - Allow interactive selection of the correct file
   - Store user's selection for future reference

### **Future Phases**

The following features will be implemented after the core functionality is stable:

### **Step 4: Handling `.xls` Legacy Files**
- Binary file formats like `.xls` cannot be searched directly with `ripgrep`.
- The application detects `.xls` files and converts them to text using **NPOI** before searching.
- A temporary file stores extracted text, which is then fed to `ripgrep`.

```csharp
public static string ConvertXlsToText(string filePath)
{
    using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
    var workbook = new HSSFWorkbook(file);
    var textContent = "";

    for (int i = 0; i < workbook.NumberOfSheets; i++)
    {
        ISheet sheet = workbook.GetSheetAt(i);
        for (int row = 0; row <= sheet.LastRowNum; row++)
        {
            IRow currentRow = sheet.GetRow(row);
            if (currentRow != null)
            {
                foreach (var cell in currentRow.Cells)
                {
                    textContent += cell.ToString() + " ";
                }
                textContent += "\n";
            }
        }
    }
    return textContent;
}
```

### **Step 5: Rank Candidate Files Based on Similarity**
- For each candidate file, surrounding lines are extracted to provide context.
- Multiple similarity measures can be applied, including **Levenshtein Distance**, **AST analysis**, and **function/class matching**.
- Users can configure the importance (weight) of each measure.

```csharp
using FuzzySharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;

class CodeSimilarityChecker
{
    public static int ComputeLevenshteinSimilarity(string originalContext, string candidateContext)
    {
        return Fuzz.Ratio(originalContext, candidateContext);
    }

    public static SyntaxNode ParseSyntaxTree(string sourceCode)
    {
        return CSharpSyntaxTree.ParseText(sourceCode).GetRoot();
    }

    // Optionally, an advanced AST-based approach could enhance accuracy:
    // public static int ComputeAstSimilarity(string originalCode, string candidateCode) { ... }

    public static List<(string filePath, int similarity)> RankCandidates(
        string originalSnippet,
        string originalContext,
        Dictionary<string, string> candidateFiles)
    {
        var rankedFiles = new List<(string, int)>();
        foreach (var file in candidateFiles)
        {
            int similarityScore = ComputeLevenshteinSimilarity(originalContext, file.Value);
            rankedFiles.Add((file.Key, similarityScore));
        }
        return rankedFiles.OrderByDescending(x => x.similarity).ToList();
    }
}
```

### **Step 6: User Experience & Customization**
The goal is to provide a smooth user experience with minimal configuration overhead while offering deep customization. Key elements include:
- **Custom Threshold**: Users can define a minimum similarity threshold.
- **Snippet Migration History**: Track each time the snippet migrates, allowing the user to see a chain of moves.
- **Partial Matches**: If exact snippet matches are not found, an option to look for near matches.
- **Interactive Mode**: The user can confirm or reject potential matches.
- **Directory/File Type Filters**: Limit searches to certain directories or file types.
- **Progress Indicators**: Provide a progress bar or console updates for large repositories.
- **Logging & Reporting**: Output JSON or YAML for machine-readable results.

### **Step 7: Integration with Git & AI**
- Implement as a standalone **C# CLI tool** invoked via `soft-follow`.
- Provide a fallback for `.xls` or other binary formats by converting to text.
- Offer an AI-based option for advanced semantic matching, possibly using large language models.
- Offer a JSON-based output that can be integrated with other dev pipelines.

**8. Deliverables**
- **C# CLI Tool**: The primary deliverable for end users.
- **Unit & Integration Tests**: Cover both typical and edge cases (e.g., large files, binary files, partial snippet matches).
- **Documentation & Tutorials**: Guidance on installation, usage, and advanced configuration.
- **Performance Benchmarks**: Testing with large repositories to ensure responsiveness.

**9. Next Steps**
- Finalize prototype that integrates dynamic `ripgrep` retrieval, `.xls` conversion, and basic similarity scoring.
- Introduce AI-based semantic analysis to handle more complex refactors.
- Add caching or concurrency features for large-scale repositories.
- Gather user feedback to refine interactive features and advanced similarity metrics.

This enhanced **Soft Follow** feature will offer a robust method for tracking refactored code snippets, bridging the gaps left by native Git rename detection and boosting developer productivity.

