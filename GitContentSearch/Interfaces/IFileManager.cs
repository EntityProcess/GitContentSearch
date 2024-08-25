using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitContentSearch.Helpers
{
    public interface IFileManager
    {
        string GenerateTempFileName(string commit, string filePath);
        void DeleteTempFile(string tempFileName);
    }
}
