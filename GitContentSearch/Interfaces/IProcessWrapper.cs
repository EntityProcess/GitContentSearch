using System.Diagnostics;

namespace GitContentSearch
{
    public interface IProcessWrapper
    {
        Process Start(ProcessStartInfo startInfo);
    }
}
