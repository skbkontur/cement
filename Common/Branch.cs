using System;
using System.Linq;

namespace Common
{
	public class Branch
	{
		public string CommitHash { get; }
		public string Name { get; }

		public Branch(string branchDescription)
		{
			var tokens = branchDescription.Split(new char[] {}, StringSplitOptions.RemoveEmptyEntries).ToArray();
			if (tokens.Length < 2 || tokens[1].IndexOf("refs/heads/") < 0)
			{
				throw new GitBranchException("Can't parse branch from:\n" + branchDescription);
			}
			CommitHash = tokens[0];
			Name = tokens[1].Replace("refs/heads/", string.Empty);
		}

	}
}
