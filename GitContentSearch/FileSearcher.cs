using System;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel; // For .xls
using NPOI.XSSF.UserModel; // For .xlsx

namespace GitContentSearch
{
    public class FileSearcher : IFileSearcher
    {
        public bool SearchInFile(string fileName, string searchString)
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

        public bool IsTextFile(string filePath)
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

        public bool SearchInTextFile(string fileName, string searchString)
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

        public bool SearchInExcel(string fileName, string searchString)
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

        public string GetCellValueAsString(ICell cell)
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
                    return cell?.ToString() ?? string.Empty;
                case CellType.Blank:
                    return string.Empty;
                default:
                    return cell?.ToString() ?? string.Empty;
            }
        }
    }
}
