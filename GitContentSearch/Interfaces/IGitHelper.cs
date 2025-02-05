namespace GitContentSearch
{
	public interface IGitHelper
	{
		string GetCommitTime(string commitHash);
		void RunGitShow(string commit, string filePath, string outputFile);
		List<Commit> GetGitCommits(string earliest, string latest);
		List<Commit> GetGitCommits(string earliest, string latest, string filePath);
	}
}