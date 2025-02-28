using System;
using System.IO;
using System.Collections.Generic;

namespace GitContentSearch.Interfaces
{
	public interface IGitHelper : IDisposable
	{
		bool IsValidRepository();
		string GetRepositoryPath();
		string GetCommitTime(string commitHash);
		bool IsValidCommit(string commitHash);
		Stream GetFileContentAtCommit(string commitHash, string filePath);
		List<Commit> GetGitCommits(string earliestCommit, string latestCommit, string filePath = "");
	}
}