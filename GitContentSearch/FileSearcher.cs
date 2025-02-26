using NPOI.HSSF.UserModel; // For .xls
using NPOI.SS.UserModel;
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

        public bool SearchInStream(Stream stream, string searchString, bool isBinary)
        {
            // Save the current position to restore it later
            long originalPosition = stream.Position;
            
            try
            {
                if (isBinary)
                {
                    stream.Position = 0;
                    // For Excel files
                    IWorkbook workbook;
                    try
                    {
                        workbook = new XSSFWorkbook(stream); // Try XLSX format first
                    }
                    catch
                    {
                        stream.Position = 0;
                        workbook = new HSSFWorkbook(stream); // Fall back to XLS format
                    }

                    using (workbook)
                    {
                        for (int i = 0; i < workbook.NumberOfSheets; i++)
                        {
                            var sheet = workbook.GetSheetAt(i);
                            if (sheet == null) continue;

                            foreach (IRow row in sheet)
                            {
                                if (row == null) continue;

                                foreach (ICell cell in row)
                                {
                                    if (cell == null) continue;

                                    var cellValue = GetCellValueAsString(cell);
                                    if (!string.IsNullOrEmpty(cellValue) && 
                                        cellValue.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    return false;
                }
                else
                {
                    // For text files
                    stream.Position = 0;
                    using var reader = new StreamReader(stream, leaveOpen: true);
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            finally
            {
                // Restore the original position
                try { stream.Position = originalPosition; } catch { }
            }
        }

        public bool IsTextFile(string filePath)
        {
            // For file paths that exist on disk
            if (File.Exists(filePath))
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return IsTextStream(stream);
            }

            // For file paths that don't exist, use extension-based detection as fallback
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".xls" or ".xlsx" => false,
                ".txt" or ".cs" or ".json" or ".xml" or ".config" or ".md" or ".yml" or ".yaml" => true,
                _ => true // Default to text for unknown extensions
            };
        }

        public bool IsTextStream(Stream stream)
        {
            const int sampleSize = 512;
            byte[] buffer = new byte[sampleSize];
            
            // Save the current position
            long originalPosition = stream.Position;
            
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                
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
                return true;
            }
            finally
            {
                // Restore the original position
                try { stream.Position = originalPosition; } catch { }
            }
        }

        public string GetCellValueAsString(ICell cell)
        {
            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.CellFormula,
                _ => string.Empty
            };
        }

        public bool SearchInTextFile(string fileName, string searchString)
        {
            try
            {
                using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                return SearchInStream(stream, searchString, false);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SearchInExcel(string fileName, string searchString)
        {
            try
            {
                using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                return SearchInStream(stream, searchString, true);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
