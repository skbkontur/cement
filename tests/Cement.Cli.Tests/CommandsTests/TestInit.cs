﻿using System.IO;
using Cement.Cli.Commands;
using Cement.Cli.Common;
using NUnit.Framework;

namespace Cement.Cli.Tests.CommandsTests;

[TestFixture]
public class TestInit
{
    [Test]
    public void CreateCementDir()
    {
        using var tmp = new TempDirectory();
        using (new DirectoryJumper(tmp.Path))
        {
            new InitCommand(ConsoleWriter.Shared).Run(new[] {"init"});
            Assert.That(Directory.Exists(Path.Combine(tmp.Path, Helper.CementDirectory)));
        }
    }
}
