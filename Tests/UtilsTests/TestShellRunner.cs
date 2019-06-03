using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using NUnit.Framework;

namespace Tests.UtilsTests
{
    [TestFixture]
    public class TestShellRunner
    {
        private readonly ShellRunner runner = new ShellRunner();

        [Test]
        public void TestRunCommand()
        {
            Assert.AreEqual(0, runner.Run(Helper.OsIsUnix() ? "ls" : "dir"));
        }

        [Test]
        public void TestRunInDirectory()
        {
            using (var temp = new TempDirectory())
            {
                runner.RunInDirectory(temp.Path, "mkdir 1");
                Assert.That(Directory.Exists(Path.Combine(temp.Path, "1")));
            }
        }

        [Test]
        public void TestUnknownCommand()
        {
            Assert.AreNotEqual(0, runner.Run("bad_command"));
        }

        [Test]
        public void TestOutputCommand()
        {
            int result = runner.Run("echo hello");
            Assert.AreEqual("hello", runner.Output.Trim());
        }

        [Test]
        public void TestRedirectOutputToError()
        {
            int result = runner.Run("echo error 1>&2");
            Assert.AreEqual("error", runner.Errors.Trim());
        }


        [Test]
        public void TestOutputChangeEvent()
        {
            var result = string.Empty;
            runner.OnOutputChange += content => result += content;
            int i = runner.Run("echo hello");
            Assert.AreEqual("hello", result);
        }

        [Test]
        public void TimeoutTest()
        {
            var sw = Stopwatch.StartNew();
            runner.Run("ping 127.0.0.1 -n 3 > nul", TimeSpan.FromSeconds(1));
            sw.Stop();
            Assert.That(sw.Elapsed.TotalSeconds > 2.5);
            Assert.That(sw.Elapsed.TotalSeconds < 4.5);
        }

        [Test]
        public async Task TimeMultiThreads()
        {
            var sw = Stopwatch.StartNew();

            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() => new ShellRunner().Run("ping 127.0.0.1 -n 2 > nul", TimeSpan.FromSeconds(1))));
            }
            await Task.WhenAll(tasks);
            sw.Stop();
            Assert.That(sw.Elapsed.TotalSeconds < 10);
        }

        [Test]
        public void TestShellRunnerOverflow()
        {
            var count = 10000;
            var bat = "test_overflow.bat";
            File.WriteAllText(bat, @"
@echo off
FOR /L %%G IN (1,1," + count + @") DO echo %%G");

            runner.Run(bat);

            var lines = runner.Output.Split('\n').ToList();
            if (lines.Last() == "")
                lines = lines.Take(lines.Count() - 1).ToList();
            CollectionAssert.AreEqual(Enumerable.Range(1, count).Select(i => i.ToString()), lines);
        }
    }
}