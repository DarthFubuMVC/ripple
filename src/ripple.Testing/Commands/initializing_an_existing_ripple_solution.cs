using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.Commands;

namespace ripple.Testing.Commands
{
    [TestFixture]
    public class initializing_an_existing_ripple_solution
    {
        private SolutionGraphScenario theScenario;

        [SetUp]
        public void SetUp()
        {
            theScenario = SolutionGraphScenario.Create(scenario =>
            {
                scenario.Solution("Test");
            });

            RippleFileSystem.StubCurrentDirectory(theScenario.DirectoryForSolution("Test"));
        }

        [TearDown]
        public void TearDown()
        {
            theScenario.Cleanup();
            RippleFileSystem.Live();
        }

        [Test]
        public void throws_when_config_exists()
        {
            Exception<RippleFatalError>.ShouldBeThrownBy(() =>
            {
                new InitCommand().Execute(new InitInput());

			}).Message.ShouldEqual(InitCommand.ExistingSolution.ToFormat(theScenario.DirectoryForSolution("Test")));
        }
    }
}