using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cement.Cli.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class TestShellRunner
{
    private readonly ShellRunner runner = new(NullLogger<ShellRunner>.Instance);

    [Test]
    [Timeout(10_000)]
    public void TestRunCommand()
    {
        // arrange
        var command = Platform.IsUnix() ? "ls" : "dir";

        // act
        var (exitCode, output, errors) = runner.Run(command);

        // assert
        exitCode.Should().Be(0, "Output: {0}, Errors: {1}", output, errors);
    }

    [Test]
    public void TestRunInDirectory()
    {
        using var temp = new TempDirectory();
        runner.RunInDirectory(temp.Path, "mkdir 1");
        Assert.That(Directory.Exists(Path.Combine(temp.Path, "1")));
    }

    [Test]
    public void TestUnknownCommand()
    {
        runner.Run("bad_command").ExitCode.Should().NotBe(0);
    }

    [Test]
    public void TestOutputCommand()
    {
        var (_, output, _) = runner.Run("echo hello");
        Assert.AreEqual("hello", output.Trim());
    }

    [Test]
    public void TestRedirectOutputToError()
    {
        var (_, _, errors) = runner.Run("echo error 1>&2");
        Assert.AreEqual("error", errors.Trim());
    }

    [Test]
    public void TestOutputChangeEvent()
    {
        var result = string.Empty;
        runner.OnOutputChange += content => result += content;
        var i = runner.Run("echo hello");
        Assert.AreEqual("hello", result);
    }

    [Test]
    // TODO fix ShellRunner, enable
    [Ignore("ShellRunner timeouts don't work")]
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
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(
                Task.Run(
                    () =>
                    {
                        return new ShellRunner(NullLogger<ShellRunner>.Instance)
                            .Run("ping 127.0.0.1 -n 2 > nul", TimeSpan.FromSeconds(1));
                    }));
        }

        await Task.WhenAll(tasks);
        sw.Stop();
        Assert.That(sw.Elapsed.TotalSeconds < 10);
    }

    [Test]
    [Platform(Include = "Win", Reason = "Uses .bat file")]
    public void TestShellRunnerOverflow()
    {
        var count = 10000;
        var bat = "test_overflow.bat";
        File.WriteAllText(
            bat, @"
@echo off
FOR /L %%G IN (1,1," + count + @") DO echo %%G");

        var (_, output, _) = runner.Run(bat);

        var lines = output.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None).ToList();
        if (lines.Last() == "")
            lines = lines.Take(lines.Count() - 1).ToList();
        CollectionAssert.AreEqual(Enumerable.Range(1, count).Select(i => i.ToString()), lines);
    }
}
