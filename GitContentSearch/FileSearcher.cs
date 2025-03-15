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

		public bool SearchInStream(Stream stream, string searchString, string extension)
		{
			if (IsTextFile(stream, extension))
			{
				return SearchInTextStream(stream, searchString);
			}
			else
			{
				return SearchInExcel(stream, searchString, extension);
			}
		}

		public bool SearchInTextStream(Stream stream, string searchString)
		{
			try
			{
				stream.Position = 0;
				using var reader = new StreamReader(stream);
				string? line;
				while ((line = reader.ReadLine()) != null)
				{
					if (line.Contains(searchString, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
			}
			catch (Exception)
			{
				return false;
			}
			return false;
		}

		public bool IsTextFile(Stream stream, string extension)
		{
			if (IsTextStream(stream))
			{
				return true;
			}

			return extension switch
			{
				".xls" or ".xlsx" => false,
				".txt" or ".cs" or ".json" or ".xml" or ".config" or ".md" or ".yml" or ".yaml" => true,
				_ => true // Default to text for unknown extensions
			};
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

        public bool SearchInExcel(Stream stream, string searchString, string extension)
        {
			try
			{
				stream.Position = 0;
				IWorkbook workbook;

				if (extension == ".xls")
				{
					workbook = new HSSFWorkbook(stream);
				}
				else if (extension == ".xlsx")
				{
					workbook = new XSSFWorkbook(stream);
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
			catch
			{
				return false;
			}

			return false;
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
			catch
			{
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
			catch
			{
				return false;
			}

			return false;
		}
	}
}
