using NPOI.SS.UserModel;

namespace GitContentSearch
{
	public interface IFileSearcher
    {
        bool SearchInStream(Stream stream, string searchString, string extension);
		bool SearchInFile(string fileName, string searchString);
        bool IsTextFile(string filePath);
        bool IsTextStream(Stream stream);
        string GetCellValueAsString(ICell cell);
        bool SearchInTextFile(string fileName, string searchString);
        bool SearchInExcel(string fileName, string searchString);
    }
}