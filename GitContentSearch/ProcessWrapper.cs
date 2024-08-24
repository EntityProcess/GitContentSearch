using System.Diagnostics;

namespace GitContentSearch
{
    public class ProcessWrapper : IProcessWrapper
    {
        public Process Start(ProcessStartInfo startInfo)
        {
            return Process.Start(startInfo);
        }
    }
}
