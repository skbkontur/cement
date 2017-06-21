using System.IO;
using System.Linq;
using Common;
using log4net;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.UtilsTests
{
	[TestFixture]
	public class TestGitRepository
    {
        private static readonly ILog Log = LogManager.GetLogger("TestBuildDepsOrder");

        private static void CreateTempRepo(TempDirectory url)
		{
			using (new DirectoryJumper(url.Path))
			{
				File.WriteAllText(Path.Combine(url.Path, "README.txt"), "README");
				var runner = new ShellRunner();
			    runner.Run("git init");
			    runner.Run("git add README.txt");
				runner.Run(@"git commit -am initial");
			}
		}

        private static void CreateNewBranchInTempRepo(TempDirectory url, string branchName)
		{
			using (new DirectoryJumper(url.Path))
			{
				var runner = new ShellRunner();
			    runner.Run("git branch " + branchName);
			}
		}

        private static void CommitIntoTempRepo(TempDirectory url, string branch)
		{
			using (new DirectoryJumper(url.Path))
			{
				var runner = new ShellRunner();
			    runner.Run("git checkout " + branch);
			    File.WriteAllText(Path.Combine(url.Path, "content.txt"), "README");
                runner.Run("git add content.txt");
                runner.Run(@"git commit -am added");
			}
		}

		[Test]
		public void TestGitClone()
		{
			using (var url = new TempDirectory())
			{
				CreateTempRepo(url);
				using (var tempDirectory = new TempDirectory())
				{
					var repo = new GitRepository("unexisting_directory", tempDirectory.Path, Log);
					repo.Clone(url.Path);
					Assert.IsTrue(Directory.Exists(Path.Combine(tempDirectory.Path, "unexisting_directory")));
				}
			}
		}

		[Test]
		public void TestGitCloneUnexistingBranch()
		{
			using (var url = new TempDirectory())
			{
				CreateTempRepo(url);
				using (var tempDirectory = new TempDirectory())
				{
				    var repo = new GitRepository("unexisting_directory", tempDirectory.Path, Log);
					Assert.Throws<GitCloneException>(() => repo.Clone(url.Path, "unexisting_branch"));
				}
			}
		}

		[Test]
		public void TestGitCloneToNotEmptyFolder()
		{
			using (var url = new TempDirectory())
			{
				CreateTempRepo(url);
				using (var tempDirectory = new TempDirectory())
				{
					Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "unexisting_directory"));
					File.WriteAllText(Path.Combine(tempDirectory.Path, "unexisting_directory", "README.txt"), "README");
					var repo = new GitRepository("unexisting_directory", tempDirectory.Path, Log);
					Assert.Throws<GitCloneException>(() => repo.Clone(url.Path));
				}
			}
		}

		[Test]
		public void TestGitCloneUnexistingRepo()
		{
            using (var tempDirectory = new TempDirectory())
            {
                var repo = new GitRepository("unexisting_directory", tempDirectory.Path, Log);
				Assert.Throws<GitCloneException>(() => repo.Clone("SOME/REPO"));
			}
		}

		[Test]
		public void TestGitTreeishDefaultIsMaster()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path), Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.AreEqual("master", repo.CurrentLocalTreeish().Value);
				Assert.AreEqual(TreeishType.Branch, repo.CurrentLocalTreeish().Type);
			}
		}

		[Test]
		public void TestGitTreeishInClearGitRepo()
		{
			using (var tempdir = new TempDirectory())
			{
                new GitRepository("unexisting_module", tempdir.Path, Log).Init();
			    var repo = new GitRepository("unexisting_module", tempdir.Path, Log);
				Assert.AreEqual("master", repo.CurrentLocalTreeish().Value);
			}
		}

		[Test]
		public void TestGitTreeishDetached()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				string sha1;
				using (new DirectoryJumper(tempRepo.Path))
				{
					var runner = new ShellRunner();
				    runner.Run("git rev-parse HEAD");
				    sha1 = runner.Output.Trim();
					runner.Run("git checkout " + sha1);
				}
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.AreEqual(sha1, repo.CurrentLocalTreeish().Value);
				Assert.AreEqual(TreeishType.CommitHash, repo.CurrentLocalTreeish().Type);
			}
		}

		[Test]
		public void TestGitTreeishOnTag()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				using (new DirectoryJumper(tempRepo.Path))
				{
					var runner = new ShellRunner();
				    runner.Run("git tag testTag");
				    runner.Run("git checkout testTag");
				}
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path), Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.AreEqual("testTag", repo.CurrentLocalTreeish().Value);
				Assert.AreEqual(TreeishType.Tag, repo.CurrentLocalTreeish().Type);
			}
		}

		[Test]
		public void TestGitCheckoutUnexistingBranch()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.Throws<GitCheckoutException>(() => repo.Checkout("unexisting_branch"));
			}
		}

		[Test]
		public void TestGitCheckoutExistingBranch()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				CreateNewBranchInTempRepo(tempRepo, "new_branch");
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				repo.Checkout("new_branch");
				Assert.AreEqual("new_branch", repo.CurrentLocalTreeish().Value);
				Assert.AreEqual(TreeishType.Branch, repo.CurrentLocalTreeish().Type);
			}
		}

		[Test]
		public void TestGitLocalChangesNoChanges()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.IsFalse(repo.HasLocalChanges());
			}
		}

		[Test]
		public void TestHasLocalChanges()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				File.WriteAllText(Path.Combine(tempRepo.Path, "content.txt"), "text");
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
					Assert.IsTrue(repo.HasLocalChanges());
			}
		}

		[Test]
		public void TestGitLocalChangesShowUntrackedFile()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				File.WriteAllText(Path.Combine(tempRepo.Path, "content.txt"), "text");
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.AreEqual("?? content.txt\n", repo.ShowLocalChanges());
			}
		}

		[Test]
		public void TestGitLocalChangesShowUpdatedFile()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				File.WriteAllText(Path.Combine(tempRepo.Path, "README.txt"), "text");
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.AreEqual(" M README.txt\n", repo.ShowLocalChanges());
			}
		}

		[Test]
		public void TestGitLocalChangesShowDeletedFile()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				File.Delete(Path.Combine(tempRepo.Path, "README.txt"));
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.AreEqual(" D README.txt\n", repo.ShowLocalChanges());
			}
		}

		[Test]
		public void TestGitCleanUntrackedFiles()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				File.WriteAllText(Path.Combine(tempRepo.Path, "content.txt"), "text");
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Helper.SetWorkspace(repo.Workspace);
				repo.Clean();
				Assert.AreEqual("", repo.ShowLocalChanges());
			}
		}

		[Test]
		public void TestGitResetModified()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				File.WriteAllText(Path.Combine(tempRepo.Path, "README.txt"), "text");
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Helper.SetWorkspace(repo.Workspace);

				var shaBefore = repo.CurrentLocalCommitHash();
				repo.ResetHard();
				var shaAfter = repo.CurrentLocalCommitHash();
				Assert.AreEqual(shaBefore, shaAfter);
				Assert.AreEqual("", repo.ShowLocalChanges());
			}
		}

		[Test]
		public void TestGitLocalCommitHash40Symbols()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.AreEqual(40, repo.CurrentLocalCommitHash().Length);
			}
		}

		[Test]
		public void TestGitLocalBranches()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				CreateNewBranchInTempRepo(tempRepo, "newbranch");
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.AreEqual(new[] { "master", "newbranch" }, repo.LocalBranches());
			}
		}

		[Test]
		public void TestGitHasLocalBranch()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				CreateNewBranchInTempRepo(tempRepo, "newbranch");
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.True(repo.HasLocalBranch("newbranch"));
			}
		}

		[Test]
		public void TestGitDoesNotHaveLocalBranch()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				CreateNewBranchInTempRepo(tempRepo, "newbranch");
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				Assert.False(repo.HasLocalBranch("unexisting_branch"));
			}
		}
		
		[Test]
		public void TestGitHasUnExistingTreeishOnRemote()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				using (var localRepo = new TempDirectory())
				{
				    var repo = new GitRepository(Path.GetFileName(localRepo.Path),
				        Directory.GetParent(localRepo.Path).FullName, Log);
                        repo.Clone(tempRepo.Path);
				}
			}
		}

		[Test]
		public void TestGitRemoteCommitHash40Symbols()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				using (var localRepo = new TempDirectory())
				{
				    var repo = new GitRepository(Path.GetFileName(localRepo.Path),
				        Directory.GetParent(localRepo.Path).FullName, Log);
						repo.Clone(tempRepo.Path);
						Assert.AreEqual(40, repo.RemoteCommitHashAtBranch("master").Length);
				}
			}
		}

		[Test]
		public void TestGitLocalCommitHashEqualsRemote()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
			    var remoteRepo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				{
					var remoteCommit = remoteRepo.CurrentLocalCommitHash();
					using (var localRepo = new TempDirectory())
					{
					    var repo = new GitRepository(Path.GetFileName(localRepo.Path),
					        Directory.GetParent(localRepo.Path).FullName, Log);
						{
                            repo.Clone(tempRepo.Path);
							var localCommit = repo.RemoteCommitHashAtBranch("master");
							Assert.AreEqual(localCommit, remoteCommit);
							Assert.AreEqual(localCommit, repo.RemoteCommitHashAtTreeish("master"));
						}
					}
				}
			}
		}

		[Test]
		public void TestGitIsKnownRemoteBranch()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				CreateNewBranchInTempRepo(tempRepo, "newbranch");
				using (var localRepo = new TempDirectory())
				{
				    var repo = new GitRepository(Path.GetFileName(localRepo.Path),
				        Directory.GetParent(localRepo.Path).FullName, Log);
					{
                        repo.Clone(tempRepo.Path);
						Assert.True(repo.IsKnownRemoteBranch("newbranch"));
					}
				}
			}
		}

		[Test]
		public void TestFetchExistingBranch()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);

			    using (var localRepo = new TempDirectory())
				{
					var repo = new GitRepository(Path.GetFileName(localRepo.Path),
					    Directory.GetParent(localRepo.Path).FullName, Log);
					{
                        repo.Clone(tempRepo.Path);

						CreateNewBranchInTempRepo(tempRepo, "newbranch");

						Assert.False(repo.IsKnownRemoteBranch("newbranch"));
						repo.Fetch("newbranch");
						Assert.True(repo.IsKnownRemoteBranch("newbranch"));
					}
				}
			}
		}

		[Test]
		public void TestGitPull()
		{
			using (var tempRepo = new TempDirectory())
			{
				CreateTempRepo(tempRepo);
				CreateNewBranchInTempRepo(tempRepo, "newbranch");

			    var remoteRepo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				{
					using (var localRepo = new TempDirectory())
					{
					    var repo = new GitRepository(Path.GetFileName(localRepo.Path),
					        Directory.GetParent(localRepo.Path).FullName, Log);
						{
                            repo.Clone(tempRepo.Path.Replace("\\", "/"));

							CommitIntoTempRepo(tempRepo, "newbranch");
							var remoteSha = remoteRepo.CurrentLocalCommitHash();
							repo.Pull("newbranch");
							var localSha = repo.CurrentLocalCommitHash();


							Assert.AreEqual(remoteSha, localSha);
						}
					}
				}
			}
		}

		[Test]
		public void TestPullForRealRepo()
		{
			using (var tempRepo = new TempDirectory())
			{
			    var repo = new GitRepository(Path.GetFileName(tempRepo.Path),
			        Directory.GetParent(tempRepo.Path).FullName, Log);
				{
                    repo.Clone("git@git.skbkontur.ru:skbkontur/cement", "master");
					repo.Fetch("release");
                    repo.HasRemoteBranch("release");
                    repo.Checkout("release");
				}
			}
		}

		[Test]
		public void TestChangeUrl()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateRepo("A");
				env.Get("A");
				env.ChangeUrl("A", "B");
			    var repo = new GitRepository("A", env.WorkingDirectory.Path, Log);
				repo.TryUpdateUrl(env.GetModules().First(d => d.Name.Equals("A")));
				Assert.AreEqual(Path.Combine(env.RemoteWorkspace, "B"), repo.RemoteOriginUrl());
			}
		}
	}
}