using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitContentSearch.Helpers
{
    public class FileManager : IFileManager
    {
        public string GenerateTempFileName(string commit, string filePath)
        {
            return $"temp_{commit}{Path.GetExtension(filePath)}";
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
