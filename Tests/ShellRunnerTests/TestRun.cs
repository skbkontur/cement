using System;
using Common;
using NSubstitute;
using NUnit.Framework;

namespace Tests.ShellRunnerTests
{
    [TestFixture]
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
            
            Assert.That(runner.Output, Is.EqualTo("current\n"), "Output is wrong");
            Assert.That(runner.Errors, Is.Empty, "Errors is wrong");
            Assert.That(runner.HasTimeout, Is.False, "HasTimeout is wrong");
            Assert.That(ShellRunnerStaticInfo.LastOutput, Is.EqualTo("current\n"), "");
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
            errorChangedEvent.Received(1).Invoke("\"wrong_command\" не является внутренней или внешней");
        }

        [Test, Explicit]
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