namespace GitContentSearch
{
    public interface IGitHelper
    {
        string[] GetGitCommits(string earliest, string latest);
        string GetCommitTime(string commitHash);
        void RunGitShow(string commit, string filePath, string outputFile);
    }
}
