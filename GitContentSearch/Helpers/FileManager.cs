using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitContentSearch.Helpers
{
    public class FileManager : IFileManager
    {
        private readonly string _directory = string.Empty;

        public FileManager(string? directory = null)
        {
            _directory = directory ?? Directory.GetCurrentDirectory();
        }

        public string GenerateTempFileName(string commit, string filePath)
        {
            return Path.Combine(_directory, $"temp_{commit}{Path.GetExtension(filePath)}");
        }

        public void DeleteTempFile(string tempFileName)
        {
            if (File.Exists(tempFileName))
            {
                File.Delete(tempFileName);
            }
        }
    }
}
