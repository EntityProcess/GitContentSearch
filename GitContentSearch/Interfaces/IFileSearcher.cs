using NPOI.SS.UserModel;

namespace GitContentSearch
{
    public interface IFileSearcher
    {
        bool SearchInFile(string fileName, string searchString);
        bool IsTextFile(string filePath);
        string GetCellValueAsString(ICell cell);
        bool SearchInTextFile(string fileName, string searchString);
        bool SearchInExcel(string fileName, string searchString);
    }
}
