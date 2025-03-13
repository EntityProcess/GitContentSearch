using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace GitContentSearch.Interfaces
{
	public interface IGitHelper : IDisposable
	{
		bool IsValidRepository();
		string GetRepositoryPath();
		string GetCommitTime(string commitHash);
		bool IsValidCommit(string commitHash);
		Stream GetFileContentAtCommit(string commitHash, string filePath);
		Stream GetFileContentAtCommit(string commitHash, string filePath, CancellationToken cancellationToken);
		List<Commit> GetGitCommits(string earliestCommit, string latestCommit, string filePath = "");
		List<Commit> GetGitCommits(string earliestCommit, string latestCommit, string filePath, CancellationToken cancellationToken);
		
		// New methods for date-based search
		List<Commit> GetGitCommitsByDate(DateTime? startDate, DateTime? endDate, string filePath = "");
		List<Commit> GetGitCommitsByDate(DateTime? startDate, DateTime? endDate, string filePath, CancellationToken cancellationToken);
		bool IsValidDate(DateTime date);
	}
}