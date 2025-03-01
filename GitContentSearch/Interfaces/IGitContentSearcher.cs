using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace GitContentSearch
{
    public interface IGitContentSearcher
    {
        void SearchContent(string filePath, string searchString, string earliestCommit = "", string latestCommit = "", IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    }
}
