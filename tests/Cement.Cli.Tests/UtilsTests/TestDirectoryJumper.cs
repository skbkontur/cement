﻿using System.IO;
using Cement.Cli.Common;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class DirectoryJumperTests
{
    [Test]
    public void TestJumpToExistingDirectory()
    {
        using var tempDir = new TempDirectory();
        using (new DirectoryJumper(tempDir.Path))
        {
            Assert.AreEqual(Directory.GetCurrentDirectory(), tempDir.Path);
        }
    }

    [Test]
    public void TestJumpBack()
    {
        var startCwd = Directory.GetCurrentDirectory();
        using (var tempDir = new TempDirectory())
        using (new DirectoryJumper(tempDir.Path))
        {
        }

        Assert.AreEqual(startCwd, Directory.GetCurrentDirectory());
    }
}
