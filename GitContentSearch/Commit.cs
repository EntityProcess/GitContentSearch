using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitContentSearch
{
	public class Commit
	{
		public Commit(string commitHash, string filePath)
		{
			CommitHash = commitHash;
			FilePath = filePath;
		}

		public string CommitHash { get; set; }
		public string FilePath{ get; set; }
	}
}
