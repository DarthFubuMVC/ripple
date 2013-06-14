using System;
using System.IO;
using NUnit.Framework;
using ripple.Commands;
using ripple.Model;
using ripple.Testing.Model;

namespace ripple.Testing.Commands
{
    [TestFixture]
    public class FindNugetsCommandTester
    {
        private SolutionGraphScenario theScenario;
        private Solution theSolution;

        [SetUp]
        public void SetUp()
        {
            FeedScenario.Create(scenario =>
            {
                scenario.For(Feed.NuGetV2)
                        .Add("FubuCore", "1.0.0.0")
                        .Add("FubuCore.Docs", "1.0.0.0")
                        .Add("log4net", "1.0.0.5")
                        .Add("log4net", "1.0.1.1");

                scenario.For(Feed.Fubu)
                    .Add("FubuCore", "1.0.0.100")
                    .Add("FubuCore.Docs", "1.0.0.100")
                    .Add("log4net", "1.0.0.5")
                    .Add("log4net", "1.0.1.1");
            });

            theScenario = SolutionGraphScenario.Create(scenario =>
            {
                scenario.Solution("Bottles", bottles =>
                {
                    bottles.SolutionDependency("FubuCore", "1.0.0.0", UpdateMode.Fixed);
                });
            });

            theSolution = theScenario.Find("Bottles");
        }

        [TearDown]
        public void TearDown()
        {
            theScenario.Cleanup();
            FeedRegistry.Reset();
        }

        [Test]
        public void query()
        {
            using (var listener = new AssertLogListener())
            {
                listener
                    .InAnyOrder()
                    .Expect(string.Format("FubuCore, 1.0.0.0 ({0})", Feed.NuGetV2.Url))
                    .Expect(string.Format("FubuCore.Docs, 1.0.0.0 ({0})", Feed.NuGetV2.Url))
                    .Expect(string.Format("FubuCore, 1.0.0.100 ({0})", Feed.Fubu.Url))
                    .Expect(string.Format("FubuCore.Docs, 1.0.0.100 ({0})", Feed.Fubu.Url));

                RippleLog.RegisterListener(listener);

                RippleOperation
                    .With(theSolution)
                    .Execute<FindNugetsInput, FindNugetsCommand>(new FindNugetsInput { Nuget = "FubuCore" });
            }
        }
    }
}