namespace GitContentSearch
{
    public interface IGitHelper
    {
        string GetCommitTime(string commitHash);
        void RunGitShow(string commit, string filePath, string outputFile);
        string[] GetGitCommits(string earliest, string latest);
        string[] GetGitCommits(string earliest, string latest, string filePath);
    }
}