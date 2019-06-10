using System.Collections.Generic;
using System.IO;
using Common;
using Common.YamlParsers;
using log4net;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.CommandsTests
{
    [TestFixture]
    public class TestGet
    {
        private static ILog Log = LogManager.GetLogger("TestModuleGetter");

        [Test]
        public void TestGetDepsSimple()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A");
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
            }
        }

        [Test]
        public void TestGetDepsSimpleWithBranch()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", null, new[] {"newbranch"});
                env.Get("A", "newbranch");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.AreEqual("newbranch", new GitRepository("A", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsOneDep()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B");
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B")));
            }
        }

        [Test]
        public void TestGetDepsOneDepWithTreeish()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B", "new")})}
                });
                env.CreateRepo("B", null, new[] {"new"});
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.AreEqual("new", new GitRepository("B", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsOneDepWithTreeishSha1()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", null, new[] {"new"});
                env.Get("A");
                var repo = new GitRepository("B", dir, Log);
                var sha1 = repo.CurrentLocalCommitHash();

                env.CommitIntoRemote("B", "newFile", "content");
                env.Get("A");

                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B", sha1)})}
                });

                env.Get("C");
                Assert.AreEqual(sha1, repo.CurrentLocalCommitHash());
            }
        }

        [Test]
        public void TestGetDepsOneDepWithForce()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(new[] {"new"}, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", null, new[] {"new"});
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.AreEqual("new", new GitRepository("B", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsOneDepWithMultipleForce()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(new[] {"priority", "new"}, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", null, new[] {"new", "priority"});
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.AreEqual("priority", new GitRepository("B", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsOneDepWithMultipleForceOneBranchMissing()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(new[] {"missing", "priority", "new"}, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", null, new[] {"new", "priority"});
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.AreEqual("priority", new GitRepository("B", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsOneDepWithResetThrowsDueToPolicy()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(new[] {"new"}, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", null, new[] {"new"});
                env.Get("A");
                env.MakeLocalChanges("B", "file", "some content");
                Assert.Throws<GitLocalChangesException>(() => env.Get("A"));
            }
        }

        [Test]
        public void TestGetDepsOneDepWithResetChanges()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(new[] {"new"}, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", null, new[] {"new"});
                env.Get("A");
                var bRepo = new GitRepository("B", dir, Log);
                env.MakeLocalChanges("B", "file", "some content");
                Assert.AreNotEqual("", bRepo.ShowLocalChanges());

                Assert.DoesNotThrow(() => env.Get("A", localChangesPolicy: LocalChangesPolicy.Reset));

                Assert.AreEqual("", bRepo.ShowLocalChanges());
            }
        }

        [Test]
        public void TestGetDepsOneDepWithResetChangesAndCommit()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(new[] {"new"}, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", null, new[] {"new"});
                env.Get("A");
                var bRepo = new GitRepository("B", dir, Log);
                var remoteSha = bRepo.RemoteCommitHashAtBranch("master");

                env.CommitIntoLocal("B", "newfile", "content");
                env.MakeLocalChanges("B", "file", "some content");

                env.Get("A", localChangesPolicy: LocalChangesPolicy.Reset);

                Assert.AreEqual("", bRepo.ShowLocalChanges());
                var newSha = bRepo.CurrentLocalCommitHash();
                Assert.AreEqual(newSha, remoteSha);
            }
        }

        [Test]
        public void TestGetDepsOneDepWithResetChangesAndMergeBase()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B");
                env.Get("A");
                var bRepo = new GitRepository("B", dir, Log);
                env.CommitIntoLocal("B", "newfile", "content");
                env.CommitIntoRemote("B", "another_new_file", "text");
                var remoteSha = bRepo.RemoteCommitHashAtBranch("master");
                env.MakeLocalChanges("B", "file", "some content");

                env.Get("A", localChangesPolicy: LocalChangesPolicy.Reset);

                Assert.AreEqual("", bRepo.ShowLocalChanges());
                var newSha = bRepo.CurrentLocalCommitHash();
                Assert.AreEqual(newSha, remoteSha);
            }
        }

        [Test]
        public void TestGetDepsOneDepWithPullAnywayFastForwardPullAllowed()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B");
                env.Get("A");
                var bRepo = new GitRepository("B", dir, Log);
                env.CommitIntoRemote("B", "another_new_file", "text");
                var remoteSha = bRepo.RemoteCommitHashAtBranch("master");
                env.MakeLocalChanges("B", "file", "some content");

                env.Get("A", localChangesPolicy: LocalChangesPolicy.Pull);

                Assert.AreNotEqual("", bRepo.ShowLocalChanges());
                var newSha = bRepo.CurrentLocalCommitHash();
                Assert.AreEqual(newSha, remoteSha);
            }
        }

        [Test]
        public void TestGetDepsOneDepWithPullAnywayFastForwardPullNotAllowedThrowsException()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B");
                env.Get("A");
                env.CommitIntoRemote("B", "file", "text");
                env.MakeLocalChanges("B", "file", "some content");

                Assert.Throws<GitPullException>(() => env.Get("A", localChangesPolicy: LocalChangesPolicy.Pull));
            }
        }

        [Test]
        public void TestGetDepsOneDepWithCurrentBranchForceOldStyle()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(new[] {"%CURRENT_BRANCH%"}, new List<Dep> {new Dep("B")})}
                }, new[] {"new"}, DepsFormatStyle.Ini);
                env.Checkout("A", "new");

                env.CreateRepo("B", null, new[] {"new"});

                env.Get("A", "new");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.AreEqual("new", new GitRepository("B", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsOneDepWithCreatingNewRemoteBranch()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(new[]  {"%CURRENT_BRANCH%"}, new List<Dep> {new Dep("B")})}
                }, new[] {"new"}, DepsFormatStyle.Ini);
                env.Checkout("A", "new");

                env.CreateRepo("B");

                env.Get("A", "new");
                env.AddBranch("B", "new");
                env.Get("A", "new");

                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.AreEqual("new", new GitRepository("B", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsOneDepWithForceAndTreeish()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(new[] {"new"}, new List<Dep> {new Dep("B")})}
                }, new[] {"new"}, DepsFormatStyle.Ini);

                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C", "new1")})}
                });

                env.CreateRepo("C", null, new[] {"new", "new1"});

                env.Get("A", "new");

                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.AreEqual("new1", new GitRepository("C", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsOneDepWithCurrentBranchForceNewStyle()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(new[] {"$CURRENT_BRANCH"}, new List<Dep> {new Dep("B")})}
                }, new[] {"new"});
                env.Checkout("A", "new");

                env.CreateRepo("B", null, new[] {"new"});

                env.Get("A", "new");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.AreEqual("new", new GitRepository("B", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsLongChain()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D")})}
                });
                env.CreateRepo("D", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("A")})}
                });
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "C")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "D")));
            }
        }

        [Test]
        public void TestGetDeps_Config_Hell()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B/client"), new Dep("C")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build *default", new DepsData(null, new List<Dep> {new Dep("D")})},
                    {"client", new DepsData(null, new List<Dep>())}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B/full-build")})}
                });
                env.CreateRepo("D", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                });
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "C")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "D")));
            }
        }

        [Test]
        public void TestGetDepsLongChainRightTreeishSwitching()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B", "1"), new Dep("C")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C")})}
                }, new[] {"1"});
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D")})}
                });
                env.CreateRepo("D", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("A")})}
                });
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "C")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "D")));
                Assert.AreEqual("1", new GitRepository("B", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsLongChainMixedDepsStyle()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C")})}
                }, null, DepsFormatStyle.Ini);
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D")})}
                }, null, DepsFormatStyle.Ini);
                env.CreateRepo("D", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("A")})}
                });
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "C")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "D")));
            }
        }

        [Test]
        public void TestGetDepsLongChainMixedDepsStyleWithConfigurations()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C", null, "client")})}
                }, null, DepsFormatStyle.Ini);
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C")})},
                    {"client *default", new DepsData(null, new List<Dep> {new Dep("C")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"*full-build", new DepsData(null, new List<Dep> {new Dep("D")})},
                    {"client", new DepsData(null, new List<Dep>())}
                }, null, DepsFormatStyle.Ini);
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "C")));
            }
        }

        [Test]
        public void TestGetDepsWithConfigurationsAndTreeishes()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C", null, "client")})}
                }, null, DepsFormatStyle.Ini);
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C")})},
                    {"client *default", new DepsData(null, new List<Dep> {new Dep("C", "notmaster")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"*full-build", new DepsData(null, new List<Dep> {new Dep("D")})},
                    {"client", new DepsData(null, new List<Dep>())}
                }, new[] {"notmaster"}, DepsFormatStyle.Ini);
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "C")));
                Assert.AreEqual("notmaster", new GitRepository("C", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestThrowsOnExplicitTreeishConflict()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C", "t1")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C", "t2")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>(), new[] {"t1", "t2"});
                Assert.Throws<TreeishConflictException>(() => env.Get("A"));
            }
        }

        [Test]
        public void TestThrowsOnMasterTreeishConflict()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C", "master")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C", "t2")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>(), new[] {"t1", "t2"});
                Assert.Throws<TreeishConflictException>(() => env.Get("A"));
            }
        }

        //Fail when running, works fine when debugging
        [Test]
        public void TestDoesNotThrowWhenDefaultTreeishAndNotDefaultTreeish1()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C", "t1")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>(), new[] {"t1"});
                Assert.DoesNotThrow(() => env.Get("A"));
            }
        }

        [Test]
        public void Test_SmallConfigToLargerConfig()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B", null, "nodomex"), new Dep("C")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build *default", new DepsData(null, new List<Dep> {new Dep("D")})},
                    {"nodomex", new DepsData(null, new List<Dep>())}
                });
                env.CreateRepo("D", new Dictionary<string, DepsData>());
                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "D")));
            }
        }

        //Fail when running, works fine when debugging
        [Test]
        public void TestDoesNotThrowWhenDefaultTreeishAndNotDefaultTreeish2()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C", "t1")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>(), new[] {"t1"});
                Assert.DoesNotThrow(() => env.Get("A"));
            }
        }

        [Test]
        public void TestGetDepsWithBranchesFullBuild()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B@master")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build *default", new DepsData(null, new List<Dep> {new Dep("C@branch1")})},
                    {"client", new DepsData(null, new List<Dep> {new Dep("C@branch2")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                }, new[] {"master", "branch1", "branch2"});

                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "C")));
                Assert.AreEqual("branch1", new GitRepository("C", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsWithBranchesClientBuild()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B@master")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C@branch1")})},
                    {"client *default", new DepsData(null, new List<Dep> {new Dep("C@branch2")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                }, new[] { "master", "branch1", "branch2" });

                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "C")));
                Assert.AreEqual("branch2", new GitRepository("C", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetDepsWithBranchSwitchToSmallerConfig()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B/full-build"), new Dep("Y")})}
                });
                env.CreateRepo("Y", new Dictionary<string, DepsData>
                {
                    {"full-build *default", new DepsData(null, new List<Dep> {new Dep("B@branch/client")})}
                });

                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build > client *default", new DepsData(null, new List<Dep> {new Dep("B1")})},
                    {"client", new DepsData(null, new List<Dep>())}
                }, new[] {"master", "branch"});

                env.Checkout("B", "branch");
                using (new DirectoryJumper(Path.Combine(env.RemoteWorkspace, "B")))
                {
                    env.CreateDepsAndCommitThem(Path.Combine(env.RemoteWorkspace, "B"), new Dictionary<string, DepsData>
                    {
                        {"full-build > client *default", new DepsData(null, new List<Dep> {new Dep("B2")})},
                        {"client", new DepsData(null, new List<Dep> {new Dep("B3")})}
                    });
                }

                env.CreateRepo("B1", new Dictionary<string, DepsData> {{"full-build *default", new DepsData(null, new List<Dep>())}});
                env.CreateRepo("B2", new Dictionary<string, DepsData> {{"full-build *default", new DepsData(null, new List<Dep>())}});
                env.CreateRepo("B3", new Dictionary<string, DepsData> {{"full-build *default", new DepsData(null, new List<Dep>())}});

                env.Get("A");

                Assert.AreEqual("branch", new GitRepository("B", dir, Log).CurrentLocalTreeish().Value);
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B1")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B2")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B3")));
            }
        }

        [Test]
        public void TestGetDepsWithBranchSwitchToHugeConfig()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B@branch/client"), new Dep("Y")})}
                });
                env.CreateRepo("Y", new Dictionary<string, DepsData>
                {
                    {"full-build *default", new DepsData(null, new List<Dep> {new Dep("B/full-build")})}
                });

                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build > client *default", new DepsData(null, new List<Dep>())},
                    {"client", new DepsData(null, new List<Dep> {new Dep("B1")})}
                }, new[] {"master", "branch"});

                env.Checkout("B", "branch");
                using (new DirectoryJumper(Path.Combine(env.RemoteWorkspace, "B")))
                {
                    env.CreateDepsAndCommitThem(Path.Combine(env.RemoteWorkspace, "B"), new Dictionary<string, DepsData>
                    {
                        {"full-build > client *default", new DepsData(null, new List<Dep> {new Dep("B2")})},
                        {"client", new DepsData(null, new List<Dep> {new Dep("B3")})}
                    });
                }

                env.CreateRepo("B1", new Dictionary<string, DepsData> {{"full-build *default", new DepsData(null, new List<Dep>())}});
                env.CreateRepo("B2", new Dictionary<string, DepsData> {{"full-build *default", new DepsData(null, new List<Dep>())}});
                env.CreateRepo("B3", new Dictionary<string, DepsData> {{"full-build *default", new DepsData(null, new List<Dep>())}});

                env.Get("A");

                Assert.AreEqual("branch", new GitRepository("B", dir, Log).CurrentLocalTreeish().Value);
                Assert.IsFalse(Directory.Exists(Path.Combine(dir, "B1")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B2")));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "B3")));
            }
        }

        [Test]
        public void TestGetOnCommitHash()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("B");
                env.CommitIntoRemote("B", "file.txt", "new commit");
                var bRemote = new GitRepository("B", env.RemoteWorkspace, Log);
                var bHash = bRemote.CurrentLocalCommitHash();

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B", bHash)})}
                });
                env.Get("A");

                Assert.IsTrue(Directory.Exists(Path.Combine(dir, "A")));
                Assert.AreEqual(bHash, new GitRepository("B", dir, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetOnCommitHashAfterPush()
        {
            using (var env = new TestEnvironment())
            {
                var cwd = env.WorkingDirectory.Path;

                env.CreateRepo("B");
                env.CommitIntoRemote("B", "file.txt", "commit 1");
                var bRemote = new GitRepository("B", env.RemoteWorkspace, Log);
                var bHash1 = bRemote.CurrentLocalCommitHash();

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B", bHash1)})}
                });

                env.Get("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(cwd, "A")));
                Assert.AreEqual(bHash1, new GitRepository("B", cwd, Log).CurrentLocalTreeish().Value);

                //push new in b
                env.CommitIntoRemote("B", "file.txt", "commit 2");
                var bHash2 = bRemote.CurrentLocalCommitHash();
                Assert.That(bHash1, Is.Not.EqualTo(bHash2));

                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B", bHash2)})}
                });

                env.Get("C");
                Assert.IsTrue(Directory.Exists(Path.Combine(cwd, "C")));
                Assert.AreEqual(bHash2, new GitRepository("B", cwd, Log).CurrentLocalTreeish().Value);
            }
        }

        [Test]
        public void TestGetInNotEmptyFolder()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                });

                env.CommitIntoRemote("A", "test.txt", "hello");

                var localA = Path.Combine(dir, "A");
                Directory.CreateDirectory(localA);
                File.WriteAllText(Path.Combine(localA, "test.txt"), "bye");

                env.Get("A");
                var newText = File.ReadAllText(Path.Combine(localA, "test.txt"));
                Assert.That(newText == "hello");
            }
        }

        [Test]
        public void TestGetInNotEmptyFolderWithExtraFile()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                });

                var localA = Path.Combine(dir, "A");
                Directory.CreateDirectory(localA);
                File.WriteAllText(Path.Combine(localA, "test.txt"), "bye");

                env.Get("A");
                Assert.That(!File.Exists(Path.Combine(localA, "test.txt")));
            }
        }

        [Test]
        public void TestGetInNotEmptyFolderWithBranch()
        {
            using (var env = new TestEnvironment())
            {
                var dir = env.WorkingDirectory.Path;

                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                });

                env.CommitIntoRemote("A", "test.txt", "hello");
                env.AddBranch("A", "new_branch");
                env.Checkout("A", "new_branch");
                env.CommitIntoRemote("A", "test.txt", "hello2");

                var localA = Path.Combine(dir, "A");
                Directory.CreateDirectory(localA);
                File.WriteAllText(Path.Combine(localA, "test.txt"), "bye");

                env.Get("A", "new_branch");
                var newText = File.ReadAllText(Path.Combine(localA, "test.txt"));
                Assert.That(newText == "hello2");
            }
        }
    }
}