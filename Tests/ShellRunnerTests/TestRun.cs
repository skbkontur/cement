using System;
using Common;
using NSubstitute;
using NUnit.Framework;

namespace Tests.ShellRunnerTests
{
    [TestFixture]
    [Platform("Windows10")]
    public class TestRun
    {
        private IShellRunner runner;
        private ReadLineEvent outputChangedEvent; 
        private ReadLineEvent errorChangedEvent;
        
        [SetUp]        
        public void Setup()
        {
            outputChangedEvent = Substitute.For<ReadLineEvent>();
            errorChangedEvent = Substitute.For<ReadLineEvent>();
            runner = ShellRunnerFactory.Create();
            runner.OnOutputChange += outputChangedEvent;
            runner.OnErrorsChange += errorChangedEvent;
        }
        
        [Test]
        public void TestExecution()
        {
            runner.Run("echo current");
            
            Assert.That(runner.Output, Is.EqualTo($"current{Environment.NewLine}"), "Output is wrong");
            Assert.That(runner.Errors, Is.Empty, "Errors is wrong");
            Assert.That(runner.HasTimeout, Is.False, "HasTimeout is wrong");
            Assert.That(ShellRunnerStaticInfo.LastOutput, Is.EqualTo($"current{Environment.NewLine}"), "LastOutput is wrong");
            outputChangedEvent.Received(1).Invoke("current");
            outputChangedEvent.DidNotReceive();
        }
        
        [Test]
        public void TestWrongCommand()
        {
            runner.Run("wrong_command", TimeSpan.FromSeconds(4));

            Assert.That(runner.Output, Is.Empty, "Output is wrong");
            Assert.That(runner.Errors, Does.StartWith("\"wrong_command\""), "Errors is wrong");
            Assert.That(runner.HasTimeout, Is.False, "HasTimeout is wrong");
            Assert.That(ShellRunnerStaticInfo.LastOutput, Is.Empty, "LastOutput is wrong");
            outputChangedEvent.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<string>());
            errorChangedEvent.Received(1).Invoke(Arg.Is<string>(line => line.StartsWith("\"wrong_command\"")));
        }

        [Test]
        [Ignore("The test does work incorrect for CliWrap and pause")]
        [Explicit("The test is too slow because default timeout increasing in IShellRunner implementations is too big")]
        public void TestTimeout()
        {
            runner.Run("pause", TimeSpan.Zero);
            
            Assert.That(runner.Output, Is.Empty, "Output is wrong");
            Assert.That(runner.Errors, Does.StartWith("Running timeout"), "Errors is wrong");
            Assert.That(runner.HasTimeout, Is.True, "HasTimeout is wrong");
            Assert.That(ShellRunnerStaticInfo.LastOutput, Is.Null, "LastOutput is wrong");
            outputChangedEvent.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<string>());
            errorChangedEvent.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<string>());
        }
    }
}