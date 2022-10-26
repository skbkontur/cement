using System;
using System.IO;
using System.Linq;
using Cement.Cli.Tests.Helpers;
using Common;
using Common.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class TestGitRepository
{
    private readonly GitRepositoryFactory gitRepositoryFactory;

    public TestGitRepository()
    {
        var consoleWriter = ConsoleWriter.Shared;
        var buildHelper = BuildHelper.Shared;

        gitRepositoryFactory = new GitRepositoryFactory(consoleWriter, buildHelper);
    }

    [Test]
    public void TestGitClone()
    {
        using var url = new TempDirectory();
        CreateTempRepo(url);
        using var tempDirectory = new TempDirectory();
        var repo = gitRepositoryFactory.Create("unexisting_directory", tempDirectory.Path);
        repo.Clone(url.Path);
        Assert.IsTrue(Directory.Exists(Path.Combine(tempDirectory.Path, "unexisting_directory")));
    }

    [Test]
    public void TestGitCloneUnexistingBranch()
    {
        using var url = new TempDirectory();
        CreateTempRepo(url);
        using var tempDirectory = new TempDirectory();
        var repo = gitRepositoryFactory.Create("unexisting_directory", tempDirectory.Path);
        Assert.Throws<GitCloneException>(() => repo.Clone(url.Path, "unexisting_branch"));
    }

    [Test]
    public void TestGitCloneToNotEmptyFolder()
    {
        using var url = new TempDirectory();
        CreateTempRepo(url);
        using var tempDirectory = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "unexisting_directory"));
        File.WriteAllText(Path.Combine(tempDirectory.Path, "unexisting_directory", "README.txt"), "README");
        var repo = gitRepositoryFactory.Create("unexisting_directory", tempDirectory.Path);
        Assert.Throws<GitCloneException>(() => repo.Clone(url.Path));
    }

    [Test]
    public void TestGitCloneUnexistingRepo()
    {
        using var tempDirectory = new TempDirectory();
        var repo = gitRepositoryFactory.Create("unexisting_directory", tempDirectory.Path);
        Assert.Throws<GitCloneException>(() => repo.Clone("SOME/REPO"));
    }

    [Test]
    public void TestGitTreeishDefaultIsMaster()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        var repo = gitRepositoryFactory.Create(Path.GetFileName(tempRepo.Path), Directory.GetParent(tempRepo.Path).FullName);
        Assert.AreEqual("master", repo.CurrentLocalTreeish().Value);
        Assert.AreEqual(TreeishType.Branch, repo.CurrentLocalTreeish().Type);
    }

    [Test]
    public void TestGitTreeishInClearGitRepo()
    {
        using var tempdir = new TempDirectory();
        gitRepositoryFactory.Create("unexisting_module", tempdir.Path).Init();
        var repo = gitRepositoryFactory.Create("unexisting_module", tempdir.Path);
        Assert.AreEqual("master", repo.CurrentLocalTreeish().Value);
    }

    [Test]
    public void TestGitTreeishDetached()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        string sha1;
        using (new DirectoryJumper(tempRepo.Path))
        {
            var runner = new ShellRunner(NullLogger<ShellRunner>.Instance);
            var (_, output, _) = runner.Run("git rev-parse HEAD");

            sha1 = output.Trim();
            runner.Run("git checkout " + sha1);
        }

        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Assert.AreEqual(sha1, repo.CurrentLocalTreeish().Value);
        Assert.AreEqual(TreeishType.CommitHash, repo.CurrentLocalTreeish().Type);
    }

    [Test]
    public void TestGitTreeishOnTag()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        using (new DirectoryJumper(tempRepo.Path))
        {
            var runner = new ShellRunner(NullLogger<ShellRunner>.Instance);
            runner.Run("git tag testTag");
            runner.Run("git checkout testTag");
        }

        var repo = gitRepositoryFactory.Create(Path.GetFileName(tempRepo.Path), Directory.GetParent(tempRepo.Path).FullName);
        Assert.AreEqual("testTag", repo.CurrentLocalTreeish().Value);
        Assert.AreEqual(TreeishType.Tag, repo.CurrentLocalTreeish().Type);
    }

    [Test]
    public void TestGitCheckoutUnexistingBranch()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Assert.Throws<GitCheckoutException>(() => repo.Checkout("unexisting_branch"));
    }

    [Test]
    public void TestGitCheckoutExistingBranch()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        CreateNewBranchInTempRepo(tempRepo, "new_branch");
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        repo.Checkout("new_branch");
        Assert.AreEqual("new_branch", repo.CurrentLocalTreeish().Value);
        Assert.AreEqual(TreeishType.Branch, repo.CurrentLocalTreeish().Type);
    }

    [Test]
    public void TestGitLocalChangesNoChanges()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Assert.IsFalse(repo.HasLocalChanges());
    }

    [Test]
    public void TestHasLocalChanges()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        File.WriteAllText(Path.Combine(tempRepo.Path, "content.txt"), "text");
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Assert.IsTrue(repo.HasLocalChanges());
    }

    [Test]
    public void TestGitLocalChangesShowUntrackedFile()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        File.WriteAllText(Path.Combine(tempRepo.Path, "content.txt"), "text");
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Assert.AreEqual("?? content.txt" + Environment.NewLine, repo.ShowLocalChanges());
    }

    [Test]
    public void TestGitLocalChangesShowUpdatedFile()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        File.WriteAllText(Path.Combine(tempRepo.Path, "README.txt"), "text");
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Assert.AreEqual(" M README.txt" + Environment.NewLine, repo.ShowLocalChanges());
    }

    [Test]
    public void TestGitLocalChangesShowDeletedFile()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        File.Delete(Path.Combine(tempRepo.Path, "README.txt"));
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Assert.AreEqual(" D README.txt" + Environment.NewLine, repo.ShowLocalChanges());
    }

    [Test]
    public void TestGitCleanUntrackedFiles()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        File.WriteAllText(Path.Combine(tempRepo.Path, "content.txt"), "text");
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Helper.SetWorkspace(repo.Workspace);
        repo.Clean();
        Assert.AreEqual("", repo.ShowLocalChanges());
    }

    [Test]
    public void TestGitResetModified()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        File.WriteAllText(Path.Combine(tempRepo.Path, "README.txt"), "text");
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Helper.SetWorkspace(repo.Workspace);

        var shaBefore = repo.CurrentLocalCommitHash();
        repo.ResetHard();
        var shaAfter = repo.CurrentLocalCommitHash();
        Assert.AreEqual(shaBefore, shaAfter);
        Assert.AreEqual("", repo.ShowLocalChanges());
    }

    [Test]
    public void TestGitLocalCommitHash40Symbols()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Assert.AreEqual(40, repo.CurrentLocalCommitHash().Length);
    }

    [Test]
    public void TestGitLocalBranches()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        CreateNewBranchInTempRepo(tempRepo, "newbranch");
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Assert.AreEqual(new[] {"master", "newbranch"}, repo.LocalBranches());
    }

    [Test]
    public void TestGitHasLocalBranch()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        CreateNewBranchInTempRepo(tempRepo, "newbranch");
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Assert.True(repo.HasLocalBranch("newbranch"));
    }

    [Test]
    public void TestGitDoesNotHaveLocalBranch()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        CreateNewBranchInTempRepo(tempRepo, "newbranch");
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        Assert.False(repo.HasLocalBranch("unexisting_branch"));
    }

    [Test]
    public void TestGitHasUnExistingTreeishOnRemote()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        using var localRepo = new TempDirectory();
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(localRepo.Path),
            Directory.GetParent(localRepo.Path).FullName);
        repo.Clone(tempRepo.Path);
    }

    [Test]
    public void TestGitRemoteCommitHash40Symbols()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        using var localRepo = new TempDirectory();
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(localRepo.Path),
            Directory.GetParent(localRepo.Path).FullName);
        repo.Clone(tempRepo.Path);
        Assert.AreEqual(40, repo.RemoteCommitHashAtBranch("master").Length);
    }

    [Test]
    public void TestGitLocalCommitHashEqualsRemote()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        var remoteRepo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        {
            var remoteCommit = remoteRepo.CurrentLocalCommitHash();
            using var localRepo = new TempDirectory();
            var repo = gitRepositoryFactory.Create(
                Path.GetFileName(localRepo.Path),
                Directory.GetParent(localRepo.Path).FullName);
            {
                repo.Clone(tempRepo.Path);
                var localCommit = repo.RemoteCommitHashAtBranch("master");
                Assert.AreEqual(localCommit, remoteCommit);
                Assert.AreEqual(localCommit, repo.RemoteCommitHashAtTreeish("master"));
            }
        }
    }

    [Test]
    public void TestGitIsKnownRemoteBranch()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        CreateNewBranchInTempRepo(tempRepo, "newbranch");
        using var localRepo = new TempDirectory();
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(localRepo.Path),
            Directory.GetParent(localRepo.Path).FullName);
        {
            repo.Clone(tempRepo.Path);
            Assert.True(repo.IsKnownRemoteBranch("newbranch"));
        }
    }

    [Test]
    public void TestFetchExistingBranch()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);

        using var localRepo = new TempDirectory();
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(localRepo.Path),
            Directory.GetParent(localRepo.Path).FullName);
        {
            repo.Clone(tempRepo.Path);

            CreateNewBranchInTempRepo(tempRepo, "newbranch");

            Assert.False(repo.IsKnownRemoteBranch("newbranch"));
            repo.Fetch("newbranch");
            Assert.True(repo.IsKnownRemoteBranch("newbranch"));
        }
    }

    [Test]
    public void TestGitPull()
    {
        using var tempRepo = new TempDirectory();
        CreateTempRepo(tempRepo);
        CreateNewBranchInTempRepo(tempRepo, "newbranch");

        var remoteRepo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        {
            using var localRepo = new TempDirectory();
            var repo = gitRepositoryFactory.Create(
                Path.GetFileName(localRepo.Path),
                Directory.GetParent(localRepo.Path).FullName);
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

    [Test]
    public void TestPullForRealRepo()
    {
        using var tempRepo = new TempDirectory();
        var repo = gitRepositoryFactory.Create(
            Path.GetFileName(tempRepo.Path),
            Directory.GetParent(tempRepo.Path).FullName);
        {
            repo.Clone("https://github.com/skbkontur/cement", "master");
            repo.Fetch("master");
            repo.HasRemoteBranch("master");
            repo.Checkout("master");
        }
    }

    [Test]
    public void TestChangeUrl()
    {
        using var env = new TestEnvironment();
        env.CreateRepo("A");
        env.Get("A");
        env.ChangeUrl("A", "B");
        var repo = gitRepositoryFactory.Create("A", env.WorkingDirectory.Path);
        repo.TryUpdateUrl(env.GetModules().First(d => d.Name.Equals("A")));
        repo.RemoteOriginUrls(out var fetchUrl, out _);
        Assert.AreEqual(Path.Combine(env.RemoteWorkspace, "B"), fetchUrl);
    }

    [Test]
    public void TestUpdatePushUrl()
    {
        using var env = new TestEnvironment();
        env.CreateRepo("A");
        env.Get("A");
        var repo = gitRepositoryFactory.Create("A", env.WorkingDirectory.Path);
        var expectedPushUrl = "C";
        var module = new Module("A", "A", expectedPushUrl);
        repo.TryUpdateUrl(module);
        repo.RemoteOriginUrls(out _, out var pushUrl);
        Assert.AreEqual(expectedPushUrl, pushUrl);
    }

    [Test]
    public void TestGetPushUrl()
    {
        using var env = new TestEnvironment();
        var expectedPushUrl = "C";
        env.CreateRepo("A", pushUrl: expectedPushUrl);
        env.Get("A");
        var repo = gitRepositoryFactory.Create("A", env.WorkingDirectory.Path);
        repo.RemoteOriginUrls(out _, out var pushUrl);
        Assert.AreEqual(expectedPushUrl, pushUrl);
    }

    private static void CreateTempRepo(TempDirectory url)
    {
        using (new DirectoryJumper(url.Path))
        {
            File.WriteAllText(Path.Combine(url.Path, "README.txt"), "README");
            var runner = new ShellRunner(NullLogger<ShellRunner>.Instance);
            runner.Run("git init");
            runner.Run("git add README.txt");
            runner.Run(@"git commit -am initial");
        }
    }

    private static void CreateNewBranchInTempRepo(TempDirectory url, string branchName)
    {
        using (new DirectoryJumper(url.Path))
        {
            var runner = new ShellRunner(NullLogger<ShellRunner>.Instance);
            runner.Run("git branch " + branchName);
        }
    }

    private static void CommitIntoTempRepo(TempDirectory url, string branch)
    {
        using (new DirectoryJumper(url.Path))
        {
            var runner = new ShellRunner(NullLogger<ShellRunner>.Instance);
            runner.Run("git checkout " + branch);
            File.WriteAllText(Path.Combine(url.Path, "content.txt"), "README");
            runner.Run("git add content.txt");
            runner.Run(@"git commit -am added");
        }
    }
}
