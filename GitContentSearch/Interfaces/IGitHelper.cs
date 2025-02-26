namespace GitContentSearch
{
	public interface IGitHelper
	{
		string GetCommitTime(string commitHash);
		List<Commit> GetGitCommits(string earliest, string latest);
		List<Commit> GetGitCommits(string earliest, string latest, string filePath);
		string GetRepositoryPath();
		bool IsValidRepository();
		bool IsValidCommit(string commitHash);
		Stream GetFileContentAtCommit(string commitHash, string filePath);
		Dictionary<string, string> GetFileHistory(string filePath);
		List<string> GetBranches();
		List<(string Hash, string Message, DateTimeOffset When)> GetCommitLog(int maxCount = 100);
	}
}