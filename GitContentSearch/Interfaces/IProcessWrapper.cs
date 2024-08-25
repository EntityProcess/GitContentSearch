using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitContentSearch
{
    public interface IProcessWrapper
    {
        ProcessResult Start(ProcessStartInfo startInfo, Stream? outputStream = null);
    }
}
